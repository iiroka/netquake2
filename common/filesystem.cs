/*
 * Copyright (C) 1997-2001 Id Software, Inc.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or (at
 * your option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
 * 02111-1307, USA.
 *
 * =======================================================================
 *
 * The Quake II file system, implements generic file system operations
 * as well as the .pak file format and support for .pk3 files.
 *
 * =======================================================================
 */
using System.Text; 

namespace Quake2 {

    partial class QCommon {

        public static string ReadString(byte[] buffer, int offset, int maxLength)
        {
            var res = new StringBuilder();

            for (int i = 0; i < maxLength && i + offset < buffer.Length && buffer[i + offset] != 0; i++)
            {
                res.Append((char)buffer[offset + i]);
            }

            return res.ToString();
        }

        private const int MAX_PAKS = 100;


        /* The .pak files are just a linear collapse of a directory tree */

        private const int IDPAKHEADER = (('K' << 24) + ('C' << 16) + ('A' << 8) + 'P');

        private record struct dpackfile_t
        {
            public readonly string name;
            public readonly int filepos, filelen;
            public const int size = 2 * 4 + 56;

            public dpackfile_t(byte[] buffer, int offset)
            {
                name = ReadString(buffer, offset, 56).ToLower();
                filepos = BitConverter.ToInt32(buffer, offset + 56);
                filelen = BitConverter.ToInt32(buffer, offset + 56 + 4);
            }

        }

        private record struct dpackheader_t
        {
            public readonly int ident; /* == IDPAKHEADER */
            public readonly int dirofs;
            public readonly int dirlen;

            public const int size = 3 * 4;

            public dpackheader_t(byte[] buffer)
            {
                ident = BitConverter.ToInt32(buffer, 0 * 4);
                dirofs = BitConverter.ToInt32(buffer, 1 * 4);
                dirlen = BitConverter.ToInt32(buffer, 2 * 4);
            }
        }

        private const int MAX_FILES_IN_PACK = 4096;


        private string datadir = "";
        private string fs_gamedir = "";
        private bool file_from_protected_pak;

        private cvar_t? fs_basedir;
        private cvar_t? fs_cddir;
        private cvar_t? fs_gamedirvar;
        private cvar_t? fs_debug;

        private record fsPackFile_t
        {
            public string name { get; init; }
            public int size { get; init; }
            public int offset { get; init; }     /* Ignored in PK3 files. */
        } ;


        private record fsPack_t
        {
            public string name { get; init; }
            public FileStream pak { get; init; }
            // unzFile *pk3;
            public bool isProtectedPak;
            public fsPackFile_t[] files { get; init; }
        }


        private record struct fsSearchPath_t
        {
            public string path { get; init; } /* Only one used. */
            public fsPack_t pack { get; init; } /* (path or pack) */
        }

        private List<fsSearchPath_t> fs_searchPaths = new List<fsSearchPath_t>();

        // --------

        // Raw search path, the actual search
        // bath is build from this one.
        private record struct fsRawPath_t {
            public string path { get; init; }
            public bool create { get; init; }
        }

        public interface IFileHandle {
            int BytesLeft();
            int Read(byte[] buffer, int offset, int size);
            void Close();
        }

        private class FileStreamHandle : IFileHandle {
            private FileStream stream;

            public FileStreamHandle(FileStream strm)
            {
                this.stream = strm;
            }

            public int BytesLeft()
            {
                return (int)(this.stream.Length - this.stream.Position);
            }

            public int Read(byte[] buffer, int offset, int size)
            {
                return this.stream.Read(buffer, 0, size);
            }

            public void Close()
            {
                this.stream.Close();
            }

        }

        private class FilePakHandle : IFileHandle {
            private FileStream stream;
            private int start;
            private int size;
            private int offset;

            public FilePakHandle(FileStream strm, int start, int size)
            {
                this.stream = strm;
                this.start = start;
                this.size = size;
                this.offset = 0;
            }

            public int BytesLeft()
            {
                return this.size - this.offset;
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                if (this.offset >= this.size) return 0;
                this.stream.Seek(this.start + this.offset, SeekOrigin.Begin);
                int n;
                if (this.offset + count < this.size)
                {
                    n = this.stream.Read(buffer, offset, count);
                } 
                else 
                {
                    n = this.stream.Read(buffer, offset, this.size - this.offset);
                }
                this.offset += n;
                return n;
            }

            public void Close()
            {
            }

        };


        /*
        * Takes an explicit (not game tree related) path to a pak file.
        *
        * Loads the header and directory, adding the files at the beginning of the
        * list so they override previous pack files.
        */
        private fsPack_t? FS_LoadPAK(string packPath)
        {
            // int i; /* Loop counter. */
            // int numFiles; /* Number of files in PAK. */
            // FILE *handle; /* File handle. */
            // fsPackFile_t *files; /* List of files in PAK. */
            // fsPack_t *pack; /* PAK file. */
            // dpackheader_t header; /* PAK file header. */
            // dpackfile_t *info = NULL; /* PAK info. */

            if (!File.Exists(packPath))
            {
                return null;
            }
            Console.WriteLine($"FileExists {packPath}");

            FileStream handle;
            try {
                handle = File.OpenRead(packPath);
            } catch (Exception) {
                return null;
            }

            var buffer = new byte[dpackheader_t.size];
            handle.Read(buffer, 0, dpackheader_t.size);

            var header = new dpackheader_t(buffer);

            if (header.ident != IDPAKHEADER)
            {
                handle.Close();
                Com_Error(QShared.ERR_FATAL, $"FS_LoadPAK: '{packPath}' is not a pack file");
            }

            int numFiles = header.dirlen / dpackfile_t.size;

            if ((numFiles == 0) || (header.dirlen < 0) || (header.dirofs < 0))
            {
                handle.Close();
                Com_Error(QShared.ERR_FATAL, $"FS_LoadPAK: '{packPath}' is too short.");
            }

            if (numFiles > MAX_FILES_IN_PACK)
            {
                Com_Printf($"FS_LoadPAK: '{packPath}' has {numFiles} > {MAX_FILES_IN_PACK} files\n");
            }

            handle.Seek(header.dirofs, SeekOrigin.Begin);
            buffer = new byte[header.dirlen];
            handle.Read(buffer, 0, header.dirlen);

            var files = new fsPackFile_t[numFiles];

            /* Parse the directory. */
            for (int i = 0; i < numFiles; i++)
            {
                var info = new dpackfile_t(buffer, i * dpackfile_t.size);
                files[i] = new fsPackFile_t()
                {
                    name = info.name,
                    offset = info.filepos,
                    size = info.filelen
                };
            }

            var pack = new fsPack_t()
            {
                name = packPath,
                pak = handle,
                files = files
            };

            Com_Printf($"Added packfile '{packPath}' ({numFiles} files).\n");

            return pack;
        }

        private void FS_AddDirToSearchPath(string dir, bool create) {
            // char *file;
            // char **list;
            // char path[MAX_OSPATH];
            // char *tmp;
            // int i, j, k;
            // int nfiles;
            // fsPack_t *pack = NULL;
            // fsSearchPath_t *search;
            // qboolean nextpak;
            // size_t len = strlen(dir);

            // // The directory must not end with an /. It would
            // // f*ck up the logic in other parts of the game...
            // if (dir[len - 1] == '/' || dir[len - 1] == '\\')
            // {
            //     dir[len - 1] = '\0';
            // }

            // Set the current directory as game directory. This
            // is somewhat fragile since the game directory MUST
            // be the last directory added to the search path.
            fs_gamedir = dir;

            // if (create)
            // {
            //     FS_CreatePath(fs_gamedir);
            // }

            // Add the directory itself.
            fs_searchPaths.Add(new fsSearchPath_t() { path = dir });


            // Numbered paks contain the official game data, they
            // need to be added first and are marked protected.
            // Files from protected paks are never offered for
            // download.
            // for (i = 0; i < sizeof(fs_packtypes) / sizeof(fs_packtypes[0]); i++)
            // {
                for (int j = 0; j < MAX_PAKS; j++)
                {
                    var path = $"{dir}/pak{j}.pak";
                    fsPack_t? pack;

            //         switch (fs_packtypes[i].format)
            //         {
            //             case PAK:
                            pack = FS_LoadPAK(path);
                            if (pack != null)
                            {
                                pack.isProtectedPak = true;
                            }

            //                 break;
            //             case PK3:
            //                 pack = FS_LoadPK3(path);

            //                 if (pack)
            //                 {
            //                     pack->isProtectedPak = false;
            //                 }

            //                 break;
            //         }

                    if (pack == null)
                    {
                        continue;
                    }

                    fs_searchPaths.Add( new fsSearchPath_t() { pack = pack });
                }
            // }

            // // All other pak files are added after the numbered paks.
            // // They aren't sorted in any way, but added in the same
            // // sequence as they're returned by FS_ListFiles. This is
            // // fragile and file system dependend. We cannot change
            // // this, since it might break existing installations.
            // for (i = 0; i < sizeof(fs_packtypes) / sizeof(fs_packtypes[0]); i++)
            // {
            //     Com_sprintf(path, sizeof(path), "%s/*.%s", dir, fs_packtypes[i].suffix);

            //     // Nothing here, next pak type please.
            //     if ((list = FS_ListFiles(path, &nfiles, 0, 0)) == NULL)
            //     {
            //         continue;
            //     }

            //     for (j = 0; j < nfiles - 1; j++)
            //     {
            //         // Sort out numbered paks. This is as inefficient as
            //         // it can be, but it doesn't matter. This is done only
            //         // once at client or game startup.
            //         nextpak = false;

            //         for (k = 0; k < MAX_PAKS; k++)
            //         {
            //             // basename() may alter the given string.
            //             // We need to work around that...
            //             tmp = strdup(list[j]);
            //             file = basename(tmp);

            //             Com_sprintf(path, sizeof(path), "pak%d.%s", k, fs_packtypes[i].suffix);

            //             if (Q_strcasecmp(path, file) == 0)
            //             {
            //                 nextpak = true;
            //                 free(tmp);
            //                 break;
            //             }

            //             free(tmp);
            //         }

            //         if (nextpak)
            //         {
            //             continue;
            //         }

            //         switch (fs_packtypes[i].format)
            //         {
            //             case PAK:
            //                 pack = FS_LoadPAK(list[j]);
            //                 break;
            //             case PK3:
            //                 pack = FS_LoadPK3(list[j]);
            //                 break;
            //         }

            //         if (pack == NULL)
            //         {
            //             continue;
            //         }

            //         pack->isProtectedPak = false;

            //         search = Z_Malloc(sizeof(fsSearchPath_t));
            //         search->pack = pack;
            //         search->next = fs_searchPaths;
            //         fs_searchPaths = search;
            //     }

            //     FS_FreeList(list, nfiles);
            // }
        }

        private void FS_BuildGenericSearchPath(List<fsRawPath_t> paths) {

            foreach (var search in paths) {
                var path = $"{search.path}/{BASEDIRNAME}";
                FS_AddDirToSearchPath(path, search.create);
            }

            // // Until here we've added the generic directories to the
            // // search path. Save the current head node so we can
            // // distinguish generic and specialized directories.
            // fs_baseSearchPaths = fs_searchPaths;

            // // We need to create the game directory.
            // Sys_Mkdir(fs_gamedir);

            // // We need to create the screenshot directory since the
            // // render dll doesn't link the filesystem stuff.
            // Com_sprintf(path, sizeof(path), "%s/scrnshot", fs_gamedir);
            // Sys_Mkdir(path);
        }

        /*
        * Finds the file in the search path. Returns filesize and an open FILE *. Used
        * for streaming data out of either a pak file or a seperate file.
        */
        public IFileHandle? FS_FOpenFile(string rawname, bool gamedir_only)
        {
        //     char path[MAX_OSPATH], lwrName[MAX_OSPATH];
        //     fsHandle_t *handle;
        //     fsPack_t *pack;
        //     fsSearchPath_t *search;
        //     int i;

            // Remove self references and empty dirs from the requested path.
            // ZIPs and PAKs don't support them, but they may be hardcoded in
            // some custom maps or models.
            var name = rawname.ToLower();
        //     char name[MAX_QPATH] = {0};
        //     size_t namelen = strlen(rawname);
        //     for (int input = 0, output = 0; input < namelen; input++)
        //     {
        //         // Remove self reference.
        //         if (rawname[input] == '.')
        //         {
        //             if (output > 0)
        //             {
        //                 // Inside the path.
        //                 if (name[output - 1] == '/' && rawname[input + 1] == '/')
        //                 {
        //                     input++;
        //                     continue;
        //                 }
        //             }
        //             else
        //             {
        //                 // At the beginning. Note: This is save because the Quake II
        //                 // VFS doesn't have a current working dir. Paths are always
        //                 // absolute.
        //                 if (rawname[input + 1] == '/')
        //                 {
        //                     continue;
        //                 }
        //             }
        //         }

        //         // Empty dir.
        //         if (rawname[input] == '/')
        //         {
        //             if (rawname[input + 1] == '/')
        //             {
        //                 continue;
        //             }
        //         }

        //         // Pathes starting with a /. I'm not sure if this is
        //         // a problem. It shouldn't hurt to remove the leading
        //         // slash, though.
        //         if (rawname[input] == '/' && output == 0)
        //         {
        //             continue;
        //         }

        //         name[output] = rawname[input];
        //         output++;
        //     }

        //     file_from_protected_pak = false;
        //     handle = FS_HandleForFile(name, f);
        //     Q_strlcpy(handle->name, name, sizeof(handle->name));
        //     handle->mode = FS_READ;

            /* Search through the path, one element at a time. */
            foreach (var search in fs_searchPaths)
            {
                // if (gamedir_only)
                // {
                //     if (strstr(search->path, FS_Gamedir()) == NULL)
                //     {
                //         continue;
                //     }
                // }

        //         // Evil hack for maps.lst and players/
        //         // TODO: A flag to ignore paks would be better
        //         if ((strcmp(fs_gamedirvar->string, "") == 0) && search->pack) {
        //             if ((strcmp(name, "maps.lst") == 0)|| (strncmp(name, "players/", 8) == 0)) {
        //                 continue;
        //             }
        //         }

                /* Search inside a pack file. */
                if (search.pack != null)
                {
                    foreach (var f in search.pack.files)
                    {
                        if (name.CompareTo(f.name) == 0)
                        {
                            /* Found it! */
                            if (fs_debug?.Bool ?? false)
                            {
                                Com_Printf($"FS_FOpenFile: '{name}' (found in '{search.pack.name}').\n");
                            }

                            return new FilePakHandle(search.pack.pak, f.offset, f.size);

        //                     // save the name with *correct case* in the handle
        //                     // (relevant for savegames, when starting map with wrong case but it's still found
        //                     //  because it's from pak, but save/bla/MAPname.sav/sv2 will have wrong case and can't be found then)
        //                     Q_strlcpy(handle->name, pack->files[i].name, sizeof(handle->name));

        //                     if (pack->pak)
        //                     {
        //                         /* PAK */
        //                         if (pack->isProtectedPak)
        //                         {
        //                             file_from_protected_pak = true;
        //                         }

        //                         handle->file = Q_fopen(pack->name, "rb");

        //                         if (handle->file)
        //                         {
        //                             fseek(handle->file, pack->files[i].offset, SEEK_SET);
        //                             return pack->files[i].size;
        //                         }
        //                     }
        //                     else if (pack->pk3)
        //                     {
        //                         /* PK3 */
        //                         if (pack->isProtectedPak)
        //                         {
        //                             file_from_protected_pak = true;
        //                         }

        // #ifdef _WIN32
        //                         handle->zip = unzOpen2(pack->name, &zlib_file_api);
        // #else
        //                         handle->zip = unzOpen(pack->name);
        // #endif

        //                         if (handle->zip)
        //                         {
        //                             if (unzLocateFile(handle->zip, handle->name, 2) == UNZ_OK)
        //                             {
        //                                 if (unzOpenCurrentFile(handle->zip) == UNZ_OK)
        //                                 {
        //                                     return pack->files[i].size;
        //                                 }
        //                             }

        //                             unzClose(handle->zip);
        //                         }
        //                     }

        //                     Com_Error(ERR_FATAL, "Couldn't reopen '%s'", pack->name);
                        }
                    }
                }
                else
                {
                    /* Search in a directory tree. */
                    var path = $"{search.path}/{name}";

                    if (File.Exists(path))
                    {
                        try {
                            var handle = File.OpenRead(path);

                            if (fs_debug?.Bool ?? false)
                            {
                                Com_Printf($"FS_FOpenFile: '{name}' (found in '{search.path}').\n");
                            }

                            return new FileStreamHandle(handle);

                        } catch (Exception) {
                        }
                    }
                }
            }
            if (fs_debug?.Bool ?? false)
            {
                Com_Printf($"FS_FOpenFile: couldn't find '{name}'.\n");
            }

            return null;
        }

        /*
        * Filename are reletive to the quake search path. A null buffer will just
        * return the file length without loading.
        */
        public byte[]? FS_LoadFile(string path)
        {
            var f = FS_FOpenFile(path, false);
            if (f == null) 
            {
                return null;
            }

            int size = f.BytesLeft();
            byte[] buf = new byte[size];
            f.Read(buf, 0, size);
            f.Close();
            return buf;
        }


        private List<fsRawPath_t> FS_BuildRawPath() {
            var result = new List<fsRawPath_t>();
            // Add $HOME/.yq2, MUST be the last dir! Required,
            // otherwise the config cannot be written.
            var homedir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            result.Add(new fsRawPath_t{ path = homedir, create = false });

            // // Add binary dir. Required, because the renderer
            // // libraries are loaded from it.
            // const char *binarydir = Sys_GetBinaryDir();

            // if(binarydir[0] != '\0')
            // {
            //     FS_AddDirToRawPath(binarydir, false, true);
            // }

            // Add data dir. Required, when the user gives us
            // a data dir he expects it in a working state.
            result.Add(new fsRawPath_t{ path = datadir, create = false });

            // Add SYSTEMDIR. Optional, the user may have a
            // binary compiled with SYSTEMWIDE (installed from
            // packages), but no systemwide game data.
        // #ifdef SYSTEMWIDE
        //     FS_AddDirToRawPath(SYSTEMDIR, false, false);
        // #endif

            // The CD must be the last directory of the path,
            // otherwise we cannot be sure that the game won't
            // stream the videos from the CD. Required, if the
            // user sets a CD path, he expects data getting
            // read from the CD.
            if (!String.IsNullOrEmpty(fs_cddir?.str)) {
                result.Add(new fsRawPath_t{ path = fs_cddir!.str, create = false });
            }
            return result; 
        }


        // --------

        private void FS_InitFilesystem()
        {
            // Register FS commands.
            // Cmd_AddCommand("path", FS_Path_f);
            // Cmd_AddCommand("link", FS_Link_f);
            // Cmd_AddCommand("dir", FS_Dir_f);

            // Register cvars
            fs_basedir = Cvar_Get("basedir", ".", cvar_t.CVAR_NOSET);
            fs_cddir = Cvar_Get("cddir", "", cvar_t.CVAR_NOSET);
            fs_gamedirvar = Cvar_Get("game", "", cvar_t.CVAR_LATCH | cvar_t.CVAR_SERVERINFO);
            fs_debug = Cvar_Get("fs_debug", "0", 0);

            // Deprecation warning, can be removed at a later time.
            if (fs_basedir?.str.CompareTo(".") != 0)
            {
                Com_Printf("+set basedir is deprecated, use -datadir instead\n");
                datadir = fs_basedir!.str;
            }
            else if (String.IsNullOrEmpty(datadir))
            {
                datadir = ".";
            }

        // #ifdef _WIN32
        //     // setup minizip for Unicode compatibility
        //     fill_fopen_filefunc(&zlib_file_api);
        //     zlib_file_api.zopen_file = fopen_file_func_utf;
        // #endif

            // Build search path
            var paths = FS_BuildRawPath();
            FS_BuildGenericSearchPath(paths);

        //     if (fs_gamedirvar->string[0] != '\0')
        //     {
        //         FS_BuildGameSpecificSearchPath(fs_gamedirvar->string);
        //     }
        // #ifndef DEDICATED_ONLY
        //     else
        //     {
        //         // no mod, but we still need to get the list of OGG tracks for background music
        //         OGG_InitTrackList();
        //     }
        // #endif

            // Debug output
            Com_Printf($"Using '{fs_gamedir}' for writing.\n");
        }

        /* .MD2 triangle model file format */

        public const int IDALIASHEADER = (('2' << 24) + ('P' << 16) + ('D' << 8) + 'I');
        public const int ALIAS_VERSION = 8;

        public const int MAX_TRIANGLES = 4096;
        public const int MAX_VERTS = 2048;
        public const int MAX_FRAMES = 512;
        public const int MAX_MD2SKINS = 32;
        public const int MAX_SKINNAME = 64;

        public record struct dstvert_t
        {
            public short s { get; }
            public short t { get; }

            public dstvert_t(byte[] buffer, int offset)
            {
                s = BitConverter.ToInt16(buffer, offset + 0 * 2);
                t = BitConverter.ToInt16(buffer, offset + 1 * 2);
            }

            public const int size = 2 * 2;
        }

        public record struct dtriangle_t
        {
            public short[] index_xyz { get; }
            public short[] index_st { get; }

            public dtriangle_t(byte[] buffer, int offset)
            {
                index_xyz = new short[3]{ 
                    BitConverter.ToInt16(buffer, offset + 0 * 2),
                    BitConverter.ToInt16(buffer, offset + 1 * 2),
                    BitConverter.ToInt16(buffer, offset + 2 * 2)
                };
                index_st = new short[3]{ 
                    BitConverter.ToInt16(buffer, offset + 3 * 2),
                    BitConverter.ToInt16(buffer, offset + 4 * 2),
                    BitConverter.ToInt16(buffer, offset + 5 * 2)
                };
            }

            public const int size = 6 * 2;
        }

        public record struct dtrivertx_t
        {
            public byte[] v; /* scaled byte to fit in frame mins/maxs */
            public byte lightnormalindex;

            public dtrivertx_t(byte[] buffer, int offset)
            {
                this.v = new byte[3]{ buffer[offset], buffer[offset + 1], buffer[offset + 2] };
                this.lightnormalindex = buffer[offset + 3];
            }

            public const int size = 4;
        }

        public const int DTRIVERTX_V0 = 0;
        public const int DTRIVERTX_V1 = 1;
        public const int DTRIVERTX_V2 = 2;
        public const int DTRIVERTX_LNI = 3;
        public const int DTRIVERTX_SIZE = 4;

        public record struct daliasframe_t
        {
            public float[] scale { get; }       /* multiply byte verts by this */
            public float[] translate { get; }   /* then add this */
            public string name { get; }         /* frame name from grabbing */
            public dtrivertx_t[] verts { get; }

            public daliasframe_t(byte[] buffer, int offset, int size)
            {
                this.scale = new float[3]{
                    BitConverter.ToSingle(buffer, offset + 0 * 4),
                    BitConverter.ToSingle(buffer, offset + 1 * 4),
                    BitConverter.ToSingle(buffer, offset + 2 * 4)
                };
                this.translate = new float[3]{
                    BitConverter.ToSingle(buffer, offset + 3 * 4),
                    BitConverter.ToSingle(buffer, offset + 4 * 4),
                    BitConverter.ToSingle(buffer, offset + 5 * 4)
                };
                this.name = ReadString(buffer, offset + 6 * 4, 16);
                this.verts = new dtrivertx_t[(size - headerSize) / dtrivertx_t.size];
                for (int i = 0; i < this.verts.Length; i++)
                {
                    this.verts[i] = new dtrivertx_t(buffer, offset + headerSize + i * dtrivertx_t.size);
                }
            }

            private const int headerSize = 16 + 6 * 4;

        }

        /* the glcmd format:
        * - a positive integer starts a tristrip command, followed by that many
        *   vertex structures.
        * - a negative integer starts a trifan command, followed by -x vertexes
        *   a zero indicates the end of the command list.
        * - a vertex consists of a floating point s, a floating point t,
        *   and an integer vertex index. */

        public record struct dmdl_t
        {
            public int ident { get; }
            public int version { get; }

            public int skinwidth { get; }
            public int skinheight { get; }
            public int framesize { get; }  /* byte size of each frame */

            public int num_skins { get; }
            public int num_xyz { get; }
            public int num_st { get; }     /* greater than num_xyz for seams */
            public int num_tris { get; }
            public int num_glcmds { get; } /* dwords in strip/fan command list */
            public int num_frames { get; }

            public int ofs_skins { get; }  /* each skin is a MAX_SKINNAME string */
            public int ofs_st { get; }     /* byte offset from start for stverts */
            public int ofs_tris { get; }   /* offset for dtriangles */
            public int ofs_frames { get; } /* offset for first frame */
            public int ofs_glcmds { get; }
            public int ofs_end { get; }    /* end of file */

            public dmdl_t(byte[] buffer, int offset)
            {
                ident = BitConverter.ToInt32(buffer, offset + 0 * 4);
                version = BitConverter.ToInt32(buffer, offset + 1 * 4);
                skinwidth = BitConverter.ToInt32(buffer, offset + 2 * 4);
                skinheight = BitConverter.ToInt32(buffer, offset + 3 * 4);
                framesize = BitConverter.ToInt32(buffer, offset + 4 * 4);
                num_skins = BitConverter.ToInt32(buffer, offset + 5 * 4);
                num_xyz = BitConverter.ToInt32(buffer, offset + 6 * 4);
                num_st = BitConverter.ToInt32(buffer, offset + 7 * 4);
                num_tris = BitConverter.ToInt32(buffer, offset + 8 * 4);
                num_glcmds = BitConverter.ToInt32(buffer, offset + 9 * 4);
                num_frames = BitConverter.ToInt32(buffer, offset + 10 * 4);
                ofs_skins = BitConverter.ToInt32(buffer, offset + 11 * 4);
                ofs_st = BitConverter.ToInt32(buffer, offset + 12 * 4);
                ofs_tris = BitConverter.ToInt32(buffer, offset + 13 * 4);
                ofs_frames = BitConverter.ToInt32(buffer, offset + 14 * 4);
                ofs_glcmds = BitConverter.ToInt32(buffer, offset + 15 * 4);
                ofs_end = BitConverter.ToInt32(buffer, offset + 16 * 4);
            }

            public const int size = 17 * 4;
        }

        /* .SP2 sprite file format */

        public const int IDSPRITEHEADER = (('2' << 24) + ('S' << 16) + ('D' << 8) + 'I'); /* little-endian "IDS2" */
        public const int SPRITE_VERSION = 2;

        // public record struct
        // {
        //     int width, height;
        //     int origin_x, origin_y;  /* raster coordinates inside pic */
        //     char name[MAX_SKINNAME]; /* name of pcx file */
        // } dsprframe_t;

        // public record struct
        // {
        //     int ident;
        //     int version;
        //     int numframes;
        //     dsprframe_t frames[1]; /* variable sized */
        // } dsprite_t;


        /* .WAL texture file format */

        public const int MIPLEVELS = 4;

        public record struct miptex_t
        {
            public string name { get; }
            public uint width { get; }
            public uint height { get; }
            public uint[] offsets { get; } /* four mip maps stored */
            public string animname { get; }           /* next frame in animation chain */
            public int flags { get; }
            public int contents { get; }
            public int value { get; }

            public miptex_t(byte[] buffer, int offset)
            {
                name = ReadString(buffer, offset, 32);
                width = BitConverter.ToUInt32(buffer, offset + 32);
                height = BitConverter.ToUInt32(buffer, offset + 32 + 4);
                offsets = new uint[MIPLEVELS];
                for (int i = 0; i < MIPLEVELS; i++) {
                    offsets[i] = BitConverter.ToUInt32(buffer, offset + 32 + (2 + i) * 4);
                }
                animname = ReadString(buffer, offset, 32 + (2 + MIPLEVELS) *4);
                flags = BitConverter.ToInt32(buffer, offset + (2 * 32) + (2 + MIPLEVELS) * 4);
                contents = BitConverter.ToInt32(buffer, offset + (2 * 32) + (3 + MIPLEVELS) * 4);
                value = BitConverter.ToInt32(buffer, offset + (2 * 32) + (4 + MIPLEVELS) * 4);
            }

            public const int size = 2 * 32 + (5 + MIPLEVELS) * 4;
        }


        /* .BSP file format */

        public const int IDBSPHEADER = (('P' << 24) + ('S' << 16) + ('B' << 8) + 'I'); /* little-endian "IBSP" */
        public const int BSPVERSION = 38;

        /* upper design bounds: leaffaces, leafbrushes, planes, and 
        * verts are still bounded by 16 bit short limits */
        public static int MAX_MAP_MODELS = 1024;
        public static int MAX_MAP_BRUSHES = 8192;
        public static int MAX_MAP_ENTITIES = 2048;
        public static int MAX_MAP_ENTSTRING = 0x40000;
        public static int MAX_MAP_TEXINFO = 8192;

        public static int MAX_MAP_AREAS = 256;
        public static int MAX_MAP_AREAPORTALS = 1024;
        public static int MAX_MAP_PLANES = 65536;
        public static int MAX_MAP_NODES = 65536;
        public static int MAX_MAP_BRUSHSIDES = 65536;
        public static int MAX_MAP_LEAFS = 65536;
        public static int MAX_MAP_VERTS = 65536;
        public static int MAX_MAP_FACES = 65536;
        public static int MAX_MAP_LEAFFACES = 65536;
        public static int MAX_MAP_LEAFBRUSHES = 65536;
        public static int MAX_MAP_PORTALS = 65536;
        public static int MAX_MAP_EDGES = 128000;
        public static int MAX_MAP_SURFEDGES = 256000;
        public static int MAX_MAP_LIGHTING = 0x200000;
        public static int MAX_MAP_VISIBILITY = 0x100000;

        /* key / value pair sizes */

        public static int MAX_KEY = 32;
        public static int MAX_VALUE = 1024;

        /* ================================================================== */

        public record struct lump_t
        {
            public int fileofs { get; }
            public int filelen { get; }

            public lump_t(byte[] buffer, int offset)
            {
                fileofs = BitConverter.ToInt32(buffer, offset);
                filelen = BitConverter.ToInt32(buffer, offset + 4);
            }

            public static int size = 2 * 4;
        }

        public static int LUMP_ENTITIES = 0;
        public static int LUMP_PLANES = 1;
        public static int LUMP_VERTEXES = 2;
        public static int LUMP_VISIBILITY = 3;
        public static int LUMP_NODES = 4;
        public static int LUMP_TEXINFO = 5;
        public static int LUMP_FACES = 6;
        public static int LUMP_LIGHTING = 7;
        public static int LUMP_LEAFS = 8;
        public static int LUMP_LEAFFACES = 9;
        public static int LUMP_LEAFBRUSHES = 10;
        public static int LUMP_EDGES = 11;
        public static int LUMP_SURFEDGES = 12;
        public static int LUMP_MODELS = 13;
        public static int LUMP_BRUSHES = 14;
        public static int LUMP_BRUSHSIDES = 15;
        public static int LUMP_POP = 16;
        public static int LUMP_AREAS = 17;
        public static int LUMP_AREAPORTALS = 18;
        public static int HEADER_LUMPS = 19;

        public record struct dheader_t
        {
            public int ident { get; }
            public int version { get; }
            public lump_t[] lumps { get; }

            public dheader_t(byte[] buffer, int offset)
            {
                this.ident = BitConverter.ToInt32(buffer, offset);
                this.version = BitConverter.ToInt32(buffer, offset + 4);
                this.lumps = new lump_t[HEADER_LUMPS];
                for (int i = 0; i < HEADER_LUMPS; i++)
                {
                    this.lumps[i] = new lump_t(buffer, offset + 2 * 4 + i * lump_t.size);
                }
            }

            public static int size = 2 * 4 + HEADER_LUMPS * lump_t.size;
        }

        public record struct dmodel_t
        {
            public float[] mins { get; }
            public float[] maxs { get; }
            public float[] origin { get; } /* for sounds or lights */
            public int headnode { get; }
            public int firstface { get; }
            public int numfaces { get; }    /* submodels just draw faces without
                                               walking the bsp tree */ 

            public dmodel_t(byte[] buffer, int offset)
            {
                this.mins = new float[3]{
                    BitConverter.ToSingle(buffer, offset + 0 * 4),
                    BitConverter.ToSingle(buffer, offset + 1 * 4),
                    BitConverter.ToSingle(buffer, offset + 2 * 4)
                };
                this.maxs = new float[3]{
                    BitConverter.ToSingle(buffer, offset + 3 * 4),
                    BitConverter.ToSingle(buffer, offset + 4 * 4),
                    BitConverter.ToSingle(buffer, offset + 5 * 4)
                };
                this.origin = new float[3]{
                    BitConverter.ToSingle(buffer, offset + 6 * 4),
                    BitConverter.ToSingle(buffer, offset + 7 * 4),
                    BitConverter.ToSingle(buffer, offset + 8 * 4)
                };
                this.headnode = BitConverter.ToInt32(buffer, offset + 9 * 4);
                this.firstface = BitConverter.ToInt32(buffer, offset + 10 * 4);
                this.numfaces = BitConverter.ToInt32(buffer, offset + 11 * 4);
            }

            public static int size = 12 * 4;
        }

        public record struct dvertex_t
        {
            public float[] point { get; }

            public dvertex_t(byte[] buffer, int offset)
            {
                this.point = new float[3]{
                    BitConverter.ToSingle(buffer, offset),
                    BitConverter.ToSingle(buffer, offset + 4),
                    BitConverter.ToSingle(buffer, offset + 2 * 4)
                };
            }

            public const int size = 3 * 4;
        }

        /* 0-2 are axial planes */
        public const int PLANE_X = 0;
        public const int PLANE_Y = 1;
        public const int PLANE_Z = 2;

        /* 3-5 are non-axial planes snapped to the nearest */
        public const int PLANE_ANYX = 3;
        public const int PLANE_ANYY = 4;
        public const int PLANE_ANYZ = 5;

        /* planes (x&~1) and (x&~1)+1 are always opposites */

        public record struct dplane_t
        {
            public float[] normal { get; }
            public float dist { get; }
            public int type { get; }    /* PLANE_X - PLANE_ANYZ */

            public dplane_t(byte[] buffer, int offset)
            {
                this.normal = new float[3]{
                    BitConverter.ToSingle(buffer, offset),
                    BitConverter.ToSingle(buffer, offset + 4),
                    BitConverter.ToSingle(buffer, offset + 2 * 4)
                };
                this.dist = BitConverter.ToSingle(buffer, offset + 3 * 4);
                this.type = BitConverter.ToInt32(buffer, offset + 4 * 4);
            }

            public const int size = 5 * 4;
        }

        /* contents flags are seperate bits
        * - given brush can contribute multiple content bits
        * - multiple brushes can be in a single leaf */

        /* lower bits are stronger, and will eat weaker brushes completely */
        public const int CONTENTS_SOLID = 1;  /* an eye is never valid in a solid */
        public const int CONTENTS_WINDOW = 2; /* translucent, but not watery */
        public const int CONTENTS_AUX = 4;
        public const int CONTENTS_LAVA = 8;
        public const int CONTENTS_SLIME = 16;
        public const int CONTENTS_WATER = 32;
        public const int CONTENTS_MIST = 64;
        public const int LAST_VISIBLE_CONTENTS = 64;

        /* remaining contents are non-visible, and don't eat brushes */
        public const int CONTENTS_AREAPORTAL = 0x8000;

        public const int CONTENTS_PLAYERCLIP = 0x10000;
        public const int CONTENTS_MONSTERCLIP = 0x20000;

        /* currents can be added to any other contents, and may be mixed */
        public const int CONTENTS_CURRENT_0 = 0x40000;
        public const int CONTENTS_CURRENT_90 = 0x80000;
        public const int CONTENTS_CURRENT_180 = 0x100000;
        public const int CONTENTS_CURRENT_270 = 0x200000;
        public const int CONTENTS_CURRENT_UP = 0x400000;
        public const int CONTENTS_CURRENT_DOWN = 0x800000;

        public const int CONTENTS_ORIGIN = 0x1000000;       /* removed before bsping an entity */

        public const int CONTENTS_MONSTER = 0x2000000;      /* should never be on a brush, only in game */
        public const int CONTENTS_DEADMONSTER = 0x4000000;
        public const int CONTENTS_DETAIL = 0x8000000;       /* brushes to be added after vis leafs */
        public const int CONTENTS_TRANSLUCENT = 0x10000000; /* auto set if any surface has trans */
        public const int CONTENTS_LADDER = 0x20000000;

        public const int SURF_LIGHT = 0x1;    /* value will hold the light strength */

        public const int SURF_SLICK = 0x2;    /* effects game physics */

        public const int SURF_SKY = 0x4;      /* don't draw, but add to skybox */
        public const int SURF_WARP = 0x8;     /* turbulent water warp */
        public const int SURF_TRANS33 = 0x10;
        public const int SURF_TRANS66 = 0x20;
        public const int SURF_FLOWING = 0x40; /* scroll towards angle */
        public const int SURF_NODRAW = 0x80;  /* don't bother referencing the texture */

        public record struct dnode_t
        {
            public int planenum { get; }
            public int[] children { get; }  /* negative numbers are -(leafs+1), not nodes */
            public short[] mins { get; }    /* for frustom culling */
            public short[] maxs { get; }
            public ushort firstface { get; }
            public ushort numfaces { get; } /* counting both sides */

            public dnode_t(byte[] buffer, int offset)
            {
                this.planenum = BitConverter.ToInt32(buffer, offset + 0 * 4);
                this.children = new int[2]{
                    BitConverter.ToInt32(buffer, offset + 1 * 4),
                    BitConverter.ToInt32(buffer, offset + 2 * 4)
                };
                this.mins = new short[3]{
                    BitConverter.ToInt16(buffer, offset + 3 * 4 + 0 * 2),
                    BitConverter.ToInt16(buffer, offset + 3 * 4 + 1 * 2),
                    BitConverter.ToInt16(buffer, offset + 3 * 4 + 2 * 2)
                };
                this.maxs = new short[3]{
                    BitConverter.ToInt16(buffer, offset + 3 * 4 + 3 * 2),
                    BitConverter.ToInt16(buffer, offset + 3 * 4 + 4 * 2),
                    BitConverter.ToInt16(buffer, offset + 3 * 4 + 5 * 2)
                };
                this.firstface = BitConverter.ToUInt16(buffer, offset + 3 * 4 + 6 * 2);
                this.numfaces = BitConverter.ToUInt16(buffer, offset + 3 * 4 + 7 * 2);
            }

            public const int size = 3 * 4 + 8 * 2;
        }

        public record struct texinfo_t
        {
            public float[][] vecs { get; } /* [s/t][xyz offset] */
            public int flags { get; }        /* miptex flags + overrides light emission, etc */
            public int value { get; }           
            public string texture { get; } /* texture name (textures*.wal) */
            public int nexttexinfo { get; }  /* for animations, -1 = end of chain */

            public texinfo_t(byte[] buffer, int offset)
            {
                this.vecs = new float[2][] {
                    new float[4]{
                        BitConverter.ToSingle(buffer, offset + 0 * 4),
                        BitConverter.ToSingle(buffer, offset + 1 * 4),
                        BitConverter.ToSingle(buffer, offset + 2 * 4),
                        BitConverter.ToSingle(buffer, offset + 3 * 4)
                    },
                    new float[4]{
                        BitConverter.ToSingle(buffer, offset + 4 * 4),
                        BitConverter.ToSingle(buffer, offset + 5 * 4),
                        BitConverter.ToSingle(buffer, offset + 6 * 4),
                        BitConverter.ToSingle(buffer, offset + 7 * 4)
                    },
                };
                this.flags = BitConverter.ToInt32(buffer, offset + 8 * 4);
                this.value = BitConverter.ToInt32(buffer, offset + 9 * 4);
                this.texture = ReadString(buffer, offset + 10 * 4, 32);
                this.nexttexinfo = BitConverter.ToInt32(buffer, offset + 10 * 4 + 32);
            }

            public const int size = 11 * 4 + 32;
        }

        /* note that edge 0 is never used, because negative edge 
        nums are used for counterclockwise use of the edge in
        a face */
        public record struct dedge_t
        {
            public ushort[] v { get; } /* vertex numbers */

            public dedge_t(byte[] buffer, int offset)
            {
                this.v = new ushort[2]{
                    BitConverter.ToUInt16(buffer, offset + 0 * 2),
                    BitConverter.ToUInt16(buffer, offset + 1 * 2)
                };
            }

            public const int size = 2 *2;
        }

        public const int MAXLIGHTMAPS = 4;
        public record struct dface_t
        {
            public ushort planenum { get; }
            public short side { get; }

            public int firstedge { get; } /* we must support > 64k edges */
            public short numedges { get; }
            public short texinfo { get; }

            /* lighting info */
            public byte[] styles  { get; }
            public int lightofs { get; } /* start of [numstyles*surfsize] samples */

            public dface_t(byte[] buffer, int offset)
            {
                this.planenum = BitConverter.ToUInt16(buffer, offset + 0 * 2);
                this.side = BitConverter.ToInt16(buffer, offset + 1 * 2);
                this.firstedge = BitConverter.ToInt32(buffer, offset + 2 * 2);
                this.numedges = BitConverter.ToInt16(buffer, offset + 2 * 2 + 4);
                this.texinfo = BitConverter.ToInt16(buffer, offset + 3 * 2 + 4);
                this.styles = new byte[MAXLIGHTMAPS];
                Array.Copy(buffer, offset + 4 * 2 + 4, this.styles, 0, MAXLIGHTMAPS);
                this.lightofs = BitConverter.ToInt32(buffer, offset + 4 * 2 + 4 + MAXLIGHTMAPS);
            }

            public const int size = 4 * 2 + 2 *4 + MAXLIGHTMAPS;
        }

        public record struct dleaf_t
        {
            public int contents { get; } /* OR of all brushes (not needed?) */

            public short cluster { get; }
            public short area { get; }

            public short[] mins { get; } /* for frustum culling */
            public short[] maxs { get; }

            public ushort firstleafface { get; }
            public ushort numleaffaces { get; }

            public ushort firstleafbrush { get; }
            public ushort numleafbrushes { get; }

            public dleaf_t(byte[] buffer, int offset)
            {
                this.contents = BitConverter.ToInt32(buffer, offset);
                this.cluster = BitConverter.ToInt16(buffer, offset + 4);
                this.area = BitConverter.ToInt16(buffer, offset + 4 + 2);
                this.mins = new short[3] {
                    BitConverter.ToInt16(buffer, offset + 4 + 2 * 2),
                    BitConverter.ToInt16(buffer, offset + 4 + 3 * 2),
                    BitConverter.ToInt16(buffer, offset + 4 + 4 * 2)
                };
                this.maxs = new short[3] {
                    BitConverter.ToInt16(buffer, offset + 4 + 5 * 2),
                    BitConverter.ToInt16(buffer, offset + 4 + 6 * 2),
                    BitConverter.ToInt16(buffer, offset + 4 + 7 * 2)
                };
                this.firstleafface = BitConverter.ToUInt16(buffer, offset + 4 + 8 * 2);
                this.numleaffaces = BitConverter.ToUInt16(buffer, offset + 4 + 9 * 2);
                this.firstleafbrush = BitConverter.ToUInt16(buffer, offset + 4 + 10 * 2);
                this.numleafbrushes = BitConverter.ToUInt16(buffer, offset + 4 + 11 * 2);
            }

            public const int size = 4 + 12 * 2;
        }

        public record struct dbrushside_t
        {
            public ushort planenum { get; } /* facing out of the leaf */
            public short texinfo { get; }

            public dbrushside_t(byte[] buffer, int offset)
            {
                this.planenum = BitConverter.ToUInt16(buffer, offset + 0 * 2);
                this.texinfo = BitConverter.ToInt16(buffer, offset + 1 * 2);
            }

            public const int size = 2 * 2;
        }

        public record struct dbrush_t
        {
            public int firstside { get; }
            public int numsides { get; }
            public int contents { get; }
            public dbrush_t(byte[] buffer, int offset)
            {
                this.firstside = BitConverter.ToInt32(buffer, offset + 0 * 4);
                this.numsides = BitConverter.ToInt32(buffer, offset + 1 * 4);
                this.contents = BitConverter.ToInt32(buffer, offset + 2 * 4);
            }

            public const int size = 3 * 4;
        }

        public static int ANGLE_UP = -1;
        public static int ANGLE_DOWN = -2;

        /* the visibility lump consists of a header with a count, then 
        * byte offsets for the PVS and PHS of each cluster, then the raw 
        * compressed bit vectors */
        public static int DVIS_PVS = 0;
        public static int DVIS_PHS = 1;
        public record struct dvis_t
        {
           public int numclusters { get; }
           public int[][] bitofs { get; } /* bitofs[numclusters][2] */

            public dvis_t(byte[] buffer, int offset)
            {
                this.numclusters = BitConverter.ToInt32(buffer, offset);
                this.bitofs = new int[numclusters][];
                for (int i = 0; i < numclusters; i++) {
                    this.bitofs[i] = new int[2]{
                        BitConverter.ToInt32(buffer, offset + (1 + 2 * i) * 4),
                        BitConverter.ToInt32(buffer, offset + (2 + 2 * i) * 4)
                    };
                }
            }

        }

        // /* each area has a list of portals that lead into other areas
        // * when portals are closed, other areas may not be visible or
        // * hearable even if the vis info says that it should be */
        public record struct dareaportal_t
        {
            public int portalnum { get; }
            public int otherarea { get; }
            public dareaportal_t(byte[] buffer, int offset)
            {
                this.portalnum = BitConverter.ToInt32(buffer, offset + 0 * 4);
                this.otherarea = BitConverter.ToInt32(buffer, offset + 1 * 4);
            }

            public const int size = 2 * 4;
        }

        public record struct darea_t
        {
            public int numareaportals { get; }
            public int firstareaportal { get; }
            public darea_t(byte[] buffer, int offset)
            {
                this.numareaportals = BitConverter.ToInt32(buffer, offset + 0 * 4);
                this.firstareaportal = BitConverter.ToInt32(buffer, offset + 1 * 4);
            }

            public const int size = 2 * 4;
        }

    }
}