using Silk.NET.OpenGL;

namespace Quake2 {

    partial class QRefGl3
    {

        private record struct glmode_t
        {
            public string name {get; init;}
            public int minimize {get; init;}
            public int maximize {get; init;}
        }

        private readonly glmode_t[] modes = {
            new glmode_t(){name="GL_NEAREST", minimize=(int)GLEnum.Nearest, maximize=(int)GLEnum.Nearest},
            new glmode_t(){name="GL_LINEAR", minimize=(int)GLEnum.Linear, maximize=(int)GLEnum.Linear},
            new glmode_t(){name="GL_NEAREST_MIPMAP_NEAREST", minimize=(int)GLEnum.NearestMipmapNearest, maximize=(int)GLEnum.Nearest},
            new glmode_t(){name="GL_LINEAR_MIPMAP_NEAREST", minimize=(int)GLEnum.LinearMipmapNearest, maximize=(int)GLEnum.Linear},
            new glmode_t(){name="GL_NEAREST_MIPMAP_LINEAR", minimize=(int)GLEnum.NearestMipmapLinear, maximize=(int)GLEnum.Nearest},
            new glmode_t(){name="GL_LINEAR_MIPMAP_LINEAR", minimize=(int)GLEnum.LinearMipmapLinear, maximize=(int)GLEnum.Linear}
        };

        private int gl_filter_min = (int)GLEnum.LinearMipmapNearest;
        private int gl_filter_max = (int)GLEnum.Linear;

        private gl3image_t[] gl3textures = new gl3image_t[1024];

        private void GL3_TextureMode(GL gl, string str)
        {
            // const int num_modes = sizeof(modes)/sizeof(modes[0]);
            int i;

            for (i = 0; i < modes.Length; i++)
            {

                if (modes[i].name.Equals(str))
                {
                    break;
                }
            }

            if (i == modes.Length)
            {
                R_Printf(QShared.PRINT_ALL, $"bad filter name '{str}' (probably from gl_texturemode)\n");
                return;
            }

            gl_filter_min = modes[i].minimize;
            gl_filter_max = modes[i].maximize;

            /* clamp selected anisotropy */
            // if (gl3config.anisotropic)
            // {
            //     if (gl_anisotropic->value > gl3config.max_anisotropy)
            //     {
            //         ri.Cvar_SetValue("r_anisotropic", gl3config.max_anisotropy);
            //     }
            // }
            // else
            // {
            //     ri.Cvar_SetValue("r_anisotropic", 0.0);
            // }

            // gl3image_t *glt;

            // const char* nolerplist = gl_nolerp_list->string;
            // const char* lerplist = r_lerp_list->string;
            // qboolean unfiltered2D = r_2D_unfiltered->value != 0;

            // /* change all the existing texture objects */
            // for (i = 0, glt = gl3textures; i < numgl3textures; i++, glt++)
            // {
            //     qboolean nolerp = false;
            //     /* r_2D_unfiltered and gl_nolerp_list allow rendering stuff unfiltered even if gl_filter_* is filtered */
            //     if (unfiltered2D && glt->type == it_pic)
            //     {
            //         // exception to that exception: stuff on the r_lerp_list
            //         nolerp = (lerplist== NULL) || (strstr(lerplist, glt->name) == NULL);
            //     }
            //     else if(nolerplist != NULL && strstr(nolerplist, glt->name) != NULL)
            //     {
            //         nolerp = true;
            //     }

            //     GL3_SelectTMU(GL_TEXTURE0);
            //     GL3_Bind(glt->texnum);
            //     if ((glt->type != it_pic) && (glt->type != it_sky)) /* mipmapped texture */
            //     {
            //         glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, gl_filter_min);
            //         glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, gl_filter_max);

            //         /* Set anisotropic filter if supported and enabled */
            //         if (gl3config.anisotropic && gl_anisotropic->value)
            //         {
            //             glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_ANISOTROPY_EXT, max(gl_anisotropic->value, 1.f));
            //         }
            //     }
            //     else /* texture has no mipmaps */
            //     {
            //         if (nolerp)
            //         {
            //             // this texture shouldn't be filtered at all (no gl_nolerp_list or r_2D_unfiltered case)
            //             glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            //             glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            //         }
            //         else
            //         {
            //             // we can't use gl_filter_min which might be GL_*_MIPMAP_*
            //             // also, there's no anisotropic filtering for textures w/o mipmaps
            //             glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, gl_filter_max);
            //             glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, gl_filter_max);
            //         }
            //     }
            // }
        }

        private void GL3_Bind(GL gl, uint texnum)
        {
            if ((gl_nobind?.Bool ?? false) && draw_chars != null) /* performance evaluation option */
            {
                texnum = draw_chars!.texnum;
            }

            if (gl3state.currenttexture == texnum)
            {
                return;
            }

            gl3state.currenttexture = texnum;
            GL3_SelectTMU(gl, TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, texnum);
        }

        private void GL3_BindLightmap(GL gl, int lightmapnum)
        {
            int i=0;
            if(lightmapnum < 0 || lightmapnum >= MAX_LIGHTMAPS)
            {
                R_Printf(QShared.PRINT_ALL, $"WARNING: Invalid lightmapnum {lightmapnum} used!\n");
                return;
            }

            if (gl3state.currentlightmap == lightmapnum)
            {
                return;
            }

            gl3state.currentlightmap = lightmapnum;
            int offset = lightmapnum * MAX_LIGHTMAPS_PER_SURFACE;
            for(i=0; i<MAX_LIGHTMAPS_PER_SURFACE; ++i)
            {
                // this assumes that GL_TEXTURE<i+1> = GL_TEXTURE<i> + 1
                // at least for GL_TEXTURE0 .. GL_TEXTURE31 that's true
                GL3_SelectTMU(gl, TextureUnit.Texture1+i);
                gl.BindTexture(TextureTarget.Texture2D, gl3state.lightmap_textureIDs[offset + i]);
            }
        }

        /*
        * Returns has_alpha
        */
        private unsafe bool GL3_Upload32(GL gl, void *data, int width, int height, bool mipmap)
        {
            // int c = width * height;
            // byte *scan = ((byte *)data) + 3;
            // InternalFormat comp = gl3_tex_solid_format;
            // PixelFormat samples = gl3_solid_format;

            // Console.WriteLine($"GL3_Upload32 {width}x{height}");

            // for (int i = 0; i < c; i++, scan += 4)
            // {
            //     if (*scan != 255)
            //     {
            //         samples = gl3_alpha_format;
            //         comp = gl3_tex_alpha_format;
            //         break;
            //     }
            // }

            // gl.TexImage2D(TextureTarget.Texture2D, 0, comp, (uint)width, (uint)height,
            //             0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width, (uint)height,
                        0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

            // var res = (samples == gl3_alpha_format);

            if (mipmap)
            {
                // TODO: some hardware may require mipmapping disabled for NPOT textures!
                gl.GenerateMipmap(TextureTarget.Texture2D);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, gl_filter_min);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, gl_filter_max);
            }
            else // if the texture has no mipmaps, we can't use gl_filter_min which might be GL_*_MIPMAP_*
            {
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, gl_filter_max);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, gl_filter_max);
            }

            if (mipmap && (gl_anisotropic?.Bool ?? false))
            {
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxAnisotropy, MathF.Max(gl_anisotropic.Float, 1.0f));
            }

            // return res;
            return true;
        }

        /*
        * Returns has_alpha
        */
        private unsafe bool GL3_Upload8(GL gl, byte[] data, int offset, int width, int height, bool mipmap, bool is_sky)
        {
            int s = width * height;
            uint[] trans = new uint[s];

            for (int i = 0; i < s; i++)
            {
                int p = data[offset + i];
                trans[i] = d_8to24table[p];

                /* transparent, so scan around for
                another color to avoid alpha fringes */
                if (p == 255)
                {
                    if ((i > width) && (data[i - width] != 255))
                    {
                        p = data[i - width];
                    }
                    else if ((i < s - width) && (data[i + width] != 255))
                    {
                        p = data[i + width];
                    }
                    else if ((i > 0) && (data[i - 1] != 255))
                    {
                        p = data[i - 1];
                    }
                    else if ((i < s - 1) && (data[i + 1] != 255))
                    {
                        p = data[i + 1];
                    }
                    else
                    {
                        p = 0;
                    }

                    /* copy rgb components */
                    trans[i] = (trans[i] & 0xFF000000) | (d_8to24table[p] & 0xFFFFFF);
                }
            }

            fixed (void* ptr = trans) {
                return GL3_Upload32(gl, ptr, width, height, mipmap);
            }
        }

        /*
        * This is also used as an entry point for the generated r_notexture
        */
        private unsafe gl3image_t GL3_LoadPic(GL gl, string name, byte[] pic, int offset, int width, int realwidth,
                    int height, int realheight, imagetype_t type, int bits)
        {
            var nolerp = false;
            if (r_2D_unfiltered?.Bool ?? false && type == imagetype_t.it_pic)
            {
                // if r_2D_unfiltered is true(ish), nolerp should usually be true,
                // *unless* the texture is on the r_lerp_list
            //     nolerp = (r_lerp_list->string == NULL) || (strstr(r_lerp_list->string, name) == NULL);
            }
            else if (!String.IsNullOrEmpty(gl_nolerp_list?.str))
            {
                nolerp = gl_nolerp_list!.str.Contains(name);
            }
            /* find a free gl3image_t */
            int i;
            for (i = 0; i < gl3textures.Length; i++)
            {
                if (gl3textures[i] == null) 
                {
                    gl3textures[i] = new gl3image_t();
                    break;
                }
                if (gl3textures[i].texnum == 0)
                {
                    break;
                }
            }

            if (i >= gl3textures.Length)
            {
                ri.Sys_Error(QShared.ERR_DROP, "MAX_GLTEXTURES");
            }

            gl3textures[i].name = name;
            gl3textures[i].registration_sequence = registration_sequence;

            gl3textures[i].width = width;
            gl3textures[i].height = height;
            gl3textures[i].type = type;

            // if ((type == it_skin) && (bits == 8))
            // {
            //     FloodFillSkin(pic, width, height);

            // }

            // image->scrap = false; // TODO: reintroduce scrap? would allow optimizations in 2D rendering..

            uint texNum = gl.GenTexture();

            gl3textures[i].texnum = texNum;

            GL3_SelectTMU(gl, TextureUnit.Texture0);
            GL3_Bind(gl, texNum);

            if (bits == 8)
            {
                // resize 8bit images only when we forced such logic
                // if (r_scale8bittextures->value)
                // {
                //     byte *image_converted;
                //     int scale = 2;

                //     // scale 3 times if lerp image
                //     if (!nolerp && (vid.height >= 240 * 3))
                //         scale = 3;

                //     image_converted = malloc(width * height * scale * scale);
                //     if (!image_converted)
                //         return NULL;

                //     if (scale == 3) {
                //         scale3x(pic, image_converted, width, height);
                //     } else {
                //         scale2x(pic, image_converted, width, height);
                //     }

                //     image->has_alpha = GL3_Upload8(image_converted, width * scale, height * scale,
                //                 (image->type != it_pic && image->type != it_sky),
                //                 image->type == it_sky);
                //     free(image_converted);
                // }
                // else
                // {
                    gl3textures[i].has_alpha = GL3_Upload8(gl, pic, offset, width, height,
                                (type != imagetype_t.it_pic && type != imagetype_t.it_sky),
                                type == imagetype_t.it_sky);
                // }
            }
            else
            {
                fixed (void* ptr = &pic[offset]) {
                    gl3textures[i].has_alpha = GL3_Upload32(gl, ptr, width, height,
                                (type != imagetype_t.it_pic && type != imagetype_t.it_sky));
                }
            }

            // if (realwidth && realheight)
            // {
            //     if ((realwidth <= image->width) && (realheight <= image->height))
            //     {
            //         image->width = realwidth;
            //         image->height = realheight;
            //     }
            //     else
            //     {
            //         R_Printf(PRINT_DEVELOPER,
            //                 "Warning, image '%s' has hi-res replacement smaller than the original! (%d x %d) < (%d x %d)\n",
            //                 name, image->width, image->height, realwidth, realheight);
            //     }
            // }

            gl3textures[i].sl = 0;
            gl3textures[i].sh = 1;
            gl3textures[i].tl = 0;
            gl3textures[i].th = 1;

            if (nolerp)
            {
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            }
        // #if 0 // TODO: the scrap could allow batch rendering 2D stuff? not sure it's worth the hassle..
        // 	/* load little pics into the scrap */
        // 	if (!nolerp && (image->type == it_pic) && (bits == 8) &&
        // 		(image->width < 64) && (image->height < 64))
        // 	{
        // 		int x, y;
        // 		int i, j, k;
        // 		int texnum;

        // 		texnum = Scrap_AllocBlock(image->width, image->height, &x, &y);

        // 		if (texnum == -1)
        // 		{
        // 			goto nonscrap;
        // 		}

        // 		scrap_dirty = true;

        // 		/* copy the texels into the scrap block */
        // 		k = 0;

        // 		for (i = 0; i < image->height; i++)
        // 		{
        // 			for (j = 0; j < image->width; j++, k++)
        // 			{
        // 				scrap_texels[texnum][(y + i) * BLOCK_WIDTH + x + j] = pic[k];
        // 			}
        // 		}

        // 		image->texnum = TEXNUM_SCRAPS + texnum;
        // 		image->scrap = true;
        // 		image->has_alpha = true;
        // 		image->sl = (x + 0.01) / (float)BLOCK_WIDTH;
        // 		image->sh = (x + image->width - 0.01) / (float)BLOCK_WIDTH;
        // 		image->tl = (y + 0.01) / (float)BLOCK_WIDTH;
        // 		image->th = (y + image->height - 0.01) / (float)BLOCK_WIDTH;
        // 	}
        // 	else
        // 	{
        // 	nonscrap:
        // 		image->scrap = false;
        // 		image->texnum = TEXNUM_IMAGES + (image - gltextures);
        // 		R_Bind(image->texnum);

        // 		if (bits == 8)
        // 		{
        // 			image->has_alpha = R_Upload8(pic, width, height,
        // 						(image->type != it_pic && image->type != it_sky),
        // 						image->type == it_sky);
        // 		}
        // 		else
        // 		{
        // 			image->has_alpha = R_Upload32((unsigned *)pic, width, height,
        // 						(image->type != it_pic && image->type != it_sky));
        // 		}

        // 		image->upload_width = upload_width; /* after power of 2 and scales */
        // 		image->upload_height = upload_height;
        // 		image->paletted = uploaded_paletted;

        // 		if (realwidth && realheight)
        // 		{
        // 			if ((realwidth <= image->width) && (realheight <= image->height))
        // 			{
        // 				image->width = realwidth;
        // 				image->height = realheight;
        // 			}
        // 			else
        // 			{
        // 				R_Printf(PRINT_DEVELOPER,
        // 						"Warning, image '%s' has hi-res replacement smaller than the original! (%d x %d) < (%d x %d)\n",
        // 						name, image->width, image->height, realwidth, realheight);
        // 			}
        // 		}

        // 		image->sl = 0;
        // 		image->sh = 1;
        // 		image->tl = 0;
        // 		image->th = 1;

        // 		if (nolerp)
        // 		{
        // 			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        // 			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        // 		}
        // 	}
        // #endif // 0
            return gl3textures[i];
        }
        private gl3image_t? LoadWal(GL gl, string origname, imagetype_t type)
        {
            // miptex_t *mt;
            // int width, height, ofs, size;
            // gl3image_t *image;
            // char name[256];

            var name = origname;

            /* Add the extension */
            if (!name.EndsWith(".wal"))
            {
                name += ".wal";
            }

            var raw = ri.FS_LoadFile(name);
            if (raw == null)
            {
                R_Printf(QShared.PRINT_ALL, $"LoadWal: can't load {name}\n");
                return gl3_notexture;
            }

            if (raw.Length < QCommon.miptex_t.size)
            {
                R_Printf(QShared.PRINT_ALL, $"LoadWal: can't load {name}, small header\n");
                return gl3_notexture;
            }

            var mt = new QCommon.miptex_t(raw, 0);
            int width = (int)mt.width;
            int height = (int)mt.height;
            int ofs = (int)mt.offsets[0];

            if ((ofs <= 0) || (width <= 0) || (height <= 0) ||
                (((raw.Length - ofs) / height) < width))
            {
                R_Printf(QShared.PRINT_ALL, $"LoadWal: can't load {name}, small body\n");
                return gl3_notexture;
            }

            return GL3_LoadPic(gl, name, raw, ofs, width, 0, height, 0, type, 8);
        }


        /*
        * Finds or loads the given image
        */
        private gl3image_t? GL3_FindImage(GL gl, string name, imagetype_t type)
        {
            // gl3image_t *image;
            // int i, len;
            // byte *pic;
            // int width, height;
            // char *ptr;
            // char namewe[256];
            // int realwidth = 0, realheight = 0;
            // const char* ext;

            if (String.IsNullOrEmpty(name))
            {
                return null;
            }

            // ext = COM_FileExtension(name);
            // if(!ext[0])
            // {
            //     /* file has no extension */
            //     return NULL;
            // }

            // len = strlen(name);

            // /* Remove the extension */
            // memset(namewe, 0, 256);
            // memcpy(namewe, name, len - (strlen(ext) + 1));

            // if (len < 5)
            // {
            //     return NULL;
            // }

            // /* fix backslashes */
            // while ((ptr = strchr(name, '\\')))
            // {
            //     *ptr = '/';
            // }

            /* look for it */
            for (int i = 0; i < gl3textures.Length; i++)
            {
                if (gl3textures[i] == null) {
                    break;
                }
                if (name.Equals(gl3textures[i].name))
                {
                    gl3textures[i].registration_sequence = registration_sequence;
                    return gl3textures[i];
                }
            }

            /* load the pic from disk */
            // pic = NULL;

            if (name.EndsWith(".pcx"))
            {
            //     if (r_retexturing->value)
            //     {
            //         GetPCXInfo(name, &realwidth, &realheight);
            //         if(realwidth == 0)
            //         {
            //             /* No texture found */
            //             return NULL;
            //         }

            //         /* try to load a tga, png or jpg (in that order/priority) */
            //         if (  LoadSTB(namewe, "tga", &pic, &width, &height)
            //         || LoadSTB(namewe, "png", &pic, &width, &height)
            //         || LoadSTB(namewe, "jpg", &pic, &width, &height) )
            //         {
            //             /* upload tga or png or jpg */
            //             image = GL3_LoadPic(name, pic, width, realwidth, height,
            //                     realheight, type, 32);
            //         }
            //         else
            //         {
            //             /* PCX if no TGA/PNG/JPEG available (exists always) */
            //             LoadPCX(name, &pic, NULL, &width, &height);

            //             if (!pic)
            //             {
            //                 /* No texture found */
            //                 return NULL;
            //             }

            //             /* Upload the PCX */
            //             image = GL3_LoadPic(name, pic, width, 0, height, 0, type, 8);
            //         }
            //     }
            //     else /* gl_retexture is not set */
            //     {
                    QPCX.Load(ri, name, out var pic, out var pal, out var width, out var height);
                    if (pic == null)
                    {
                        return null;
                    }

                    return GL3_LoadPic(gl, name, pic, 0, width, 0, height, 0, type, 8);
            //     }
            }
            else if (name.EndsWith(".wal") || name.EndsWith(".m8"))
            {
            //     if (r_retexturing->value)
            //     {
            //         /* Get size of the original texture */
            //         if (strcmp(ext, "m8") == 0)
            //         {
            //             GetM8Info(name, &realwidth, &realheight);
            //         }
            //         else
            //         {
            //             GetWalInfo(name, &realwidth, &realheight);
            //         }

            //         if(realwidth == 0)
            //         {
            //             /* No texture found */
            //             return NULL;
            //         }

            //         /* try to load a tga, png or jpg (in that order/priority) */
            //         if (  LoadSTB(namewe, "tga", &pic, &width, &height)
            //         || LoadSTB(namewe, "png", &pic, &width, &height)
            //         || LoadSTB(namewe, "jpg", &pic, &width, &height) )
            //         {
            //             /* upload tga or png or jpg */
            //             image = GL3_LoadPic(name, pic, width, realwidth, height, realheight, type, 32);
            //         }
            //         else 
                    if (name.EndsWith(".m8"))
                    {
            //             image = LoadM8(namewe, type);
                    }
                    else
                    {
                        /* WAL if no TGA/PNG/JPEG available (exists always) */
                        return LoadWal(gl, name, type);
                    }
            //     }
            //     else if (strcmp(ext, "m8") == 0)
            //     {
            //         image = LoadM8(name, type);

            //         if (!image)
            //         {
            //             /* No texture found */
            //             return NULL;
            //         }
            //     }
            //     else /* gl_retexture is not set */
            //     {
            //         image = LoadWal(name, type);

            //         if (!image)
            //         {
            //             /* No texture found */
            //             return NULL;
            //         }
            //     }
            }
            // else if (strcmp(ext, "tga") == 0 || strcmp(ext, "png") == 0 || strcmp(ext, "jpg") == 0)
            // {
            //     char tmp_name[256];

            //     realwidth = 0;
            //     realheight = 0;

            //     strcpy(tmp_name, namewe);
            //     strcat(tmp_name, ".wal");
            //     GetWalInfo(tmp_name, &realwidth, &realheight);

            //     if (realwidth == 0 || realheight == 0) {
            //         strcpy(tmp_name, namewe);
            //         strcat(tmp_name, ".m8");
            //         GetM8Info(tmp_name, &realwidth, &realheight);
            //     }

            //     if (realwidth == 0 || realheight == 0) {
            //         /* It's a sky or model skin. */
            //         strcpy(tmp_name, namewe);
            //         strcat(tmp_name, ".pcx");
            //         GetPCXInfo(tmp_name, &realwidth, &realheight);
            //     }

            //     /* TODO: not sure if not having realwidth/heigth is bad - a tga/png/jpg
            //     * was requested, after all, so there might be no corresponding wal/pcx?
            //     * if (realwidth == 0 || realheight == 0) return NULL;
            //     */

            //     if(LoadSTB(name, ext, &pic, &width, &height))
            //     {
            //         image = GL3_LoadPic(name, pic, width, realwidth, height, realheight, type, 32);
            //     } else {
            //         return NULL;
            //     }
            // }
            // else
            // {
            //     return NULL;
            // }

            // if (pic)
            // {
            //     free(pic);
            // }

            Console.WriteLine($"Cannot load image {name}");
            return null;
        }

        public image_s? RegisterSkin (Silk.NET.Windowing.IWindow window, string name)
        {
            return GL3_FindImage(GL.GetApi(window), name, imagetype_t.it_skin);
        }

    }
}
