namespace Quake2 {

    internal class menuframework_s
    {
        public int x, y;
        public int cursor;

        // int nitems;
        public List<menucommon_s> items;
        // void *items[64];

        // public string statusbar;

        // void (*cursordraw)(struct _tag_menuframework *m);

        public QClient client;

        protected menuframework_s(QClient client)
        {
            this.items = new List<menucommon_s>();
            this.client = client;
        }

        protected void AddItem(menucommon_s item)
        {
            items.Add(item);
            item.parent = this;
        }

        protected void Center()
        {
            // int height;
            // float scale = SCR_GetMenuScale();
            float scale = 1.0f;

            int height = items.Last().y;
            height += 10;

            this.y = ((int)(client.vid.viddef.height / scale) - height) / 2;
        }

        protected void Draw()
        {
            // int i;
            // menucommon_s *item;
            // float scale = SCR_GetMenuScale();

            /* draw contents */
            for (int i = 0; i < items.Count(); i++)
            {
                items[i].Draw(i == cursor);
                // switch (items[i])
                // {
            //         case MTYPE_FIELD:
            //             Field_Draw((menufield_s *)menu->items[i]);
            //             break;
            //         case MTYPE_SLIDER:
            //             Slider_Draw((menuslider_s *)menu->items[i]);
            //             break;
            //         case MTYPE_LIST:
            //             MenuList_Draw((menulist_s *)menu->items[i]);
            //             break;
            //         case MTYPE_SPINCONTROL:
            //             SpinControl_Draw((menulist_s *)menu->items[i]);
            //             break;
                    // case typeof(menubitmap_s):
            //             Bitmap_Draw(( menubitmap_s * )menu->items[i]);
                        // break;
            //         case MTYPE_ACTION:
            //             Action_Draw((menuaction_s *)menu->items[i]);
            //             break;
            //         case MTYPE_SEPARATOR:
            //             Separator_Draw((menuseparator_s *)menu->items[i]);
            //             break;
                // }
            }

            // item = Menu_ItemAtCursor(menu);

            // if (item && item->cursordraw)
            // {
            //     item->cursordraw(item);
            // }
            // else if (menu->cursordraw)
            // {
            //     menu->cursordraw(menu);
            // }
            // else if (item && (item->type != MTYPE_FIELD) && item->type != MTYPE_BITMAP)
            // {
            //     if (item->flags & QMF_LEFT_JUSTIFY)
            //     {
            //         Draw_CharScaled(menu->x + (item->x / scale - 24 + item->cursor_offset) * scale,
            //                 (menu->y + item->y) * scale,
            //                 12 + ((int)(Sys_Milliseconds() / 250) & 1), scale);
            //     }
            //     else
            //     {
            //         // FIXME:: menu->x + (item->x / scale + 24 + item->cursor_offset) * scale
            //         Draw_CharScaled(menu->x + (item->cursor_offset) * scale,
            //                 (menu->y + item->y) * scale,
            //                 12 + ((int)(Sys_Milliseconds() / 250) & 1), scale);
            //     }
            // }

            // if (item)
            // {
            //     if (item->statusbarfunc)
            //     {
            //         item->statusbarfunc((void *)item);
            //     }

            //     else if (item->statusbar)
            //     {
            //         Menu_DrawStatusBar(item->statusbar);
            //     }

            //     else
            //     {
            //         Menu_DrawStatusBar(menu->statusbar);
            //     }
            // }
            // else
            // {
            //     Menu_DrawStatusBar(menu->statusbar);
            // }
        }

        public static int GetMenuKey(int key)
        {
            switch (key)
            {
                case (int)QClient.QKEYS.K_KP_UPARROW:
                case (int)QClient.QKEYS.K_UPARROW:
                case (int)QClient.QKEYS.K_DPAD_UP:
                    return (int)QClient.QKEYS.K_UPARROW;

                case (int)QClient.QKEYS.K_TAB:
                case (int)QClient.QKEYS.K_KP_DOWNARROW:
                case (int)QClient.QKEYS.K_DOWNARROW:
                case (int)QClient.QKEYS.K_DPAD_DOWN:
                    return (int)QClient.QKEYS.K_DOWNARROW;

                case (int)QClient.QKEYS.K_KP_LEFTARROW:
                case (int)QClient.QKEYS.K_LEFTARROW:
                case (int)QClient.QKEYS.K_DPAD_LEFT:
                case (int)QClient.QKEYS.K_SHOULDER_LEFT:
                    return (int)QClient.QKEYS.K_LEFTARROW;

                case (int)QClient.QKEYS.K_KP_RIGHTARROW:
                case (int)QClient.QKEYS.K_RIGHTARROW:
                case (int)QClient.QKEYS.K_DPAD_RIGHT:
                case (int)QClient.QKEYS.K_SHOULDER_RIGHT:
                    return (int)QClient.QKEYS.K_RIGHTARROW;

                case (int)QClient.QKEYS.K_MOUSE1:
                case (int)QClient.QKEYS.K_MOUSE2:
                case (int)QClient.QKEYS.K_MOUSE3:
                case (int)QClient.QKEYS.K_MOUSE4:
                case (int)QClient.QKEYS.K_MOUSE5:

                case (int)QClient.QKEYS.K_KP_ENTER:
                case (int)QClient.QKEYS.K_ENTER:
                case (int)QClient.QKEYS.K_BTN_A:
                    return (int)QClient.QKEYS.K_ENTER;

                case (int)QClient.QKEYS.K_ESCAPE:
                case (int)QClient.QKEYS.K_JOY_BACK:
                case (int)QClient.QKEYS.K_BTN_B:
                    return (int)QClient.QKEYS.K_ESCAPE;

                case (int)QClient.QKEYS.K_BACKSPACE:
                case (int)QClient.QKEYS.K_DEL:
                case (int)QClient.QKEYS.K_KP_DEL:
                case (int)QClient.QKEYS.K_BTN_Y:
                    return (int)QClient.QKEYS.K_BACKSPACE;
            }

            return key;
        }

        public string? key(int key)
        {
            string? sound = null;
            int menu_key = GetMenuKey(key);

            Console.WriteLine($"Key {(QClient.QKEYS)menu_key}");

            // if (m)
            // {
            //     menucommon_s *item;

            //     if ((item = Menu_ItemAtCursor(m)) != 0)
            //     {
            //         if (item->type == MTYPE_FIELD)
            //         {
            //             if (Field_Key((menufield_s *)item, key))
            //             {
            //                 return NULL;
            //             }
            //         }
            //     }
            // }

            switch (menu_key)
            {
            // case K_ESCAPE:
            //     M_PopMenu();
            //     return menu_out_sound;

                case (int)QClient.QKEYS.K_UPARROW:
                    cursor--;
                    AdjustCursor(-1);
            //         sound = menu_move_sound;
                    break;

                case (int)QClient.QKEYS.K_DOWNARROW:
                    cursor++;
                    AdjustCursor(1);
                    // sound = menu_move_sound;
                    break;

            // case K_LEFTARROW:
            //     if (m)
            //     {
            //         Menu_SlideItem(m, -1);
            //         sound = menu_move_sound;
            //     }
            //     break;

            // case K_RIGHTARROW:
            //     if (m)
            //     {
            //         Menu_SlideItem(m, 1);
            //         sound = menu_move_sound;
            //     }
            //     break;

                case (int)QClient.QKEYS.K_ENTER:
                    if (cursor >= 0 && cursor < items.Count)
                    {
                        items[cursor].callback!(items[cursor]);
	                }
            //     if (m)
            //     {
            //         Menu_SelectItem(m);
            //     }
            //     sound = menu_move_sound;
                    break;
            }

            return sound;
        }

        /*
        * This function takes the given menu, the direction, and attempts
        * to adjust the menu's cursor so that it's at the next available
        * slot.
        */
        public void AdjustCursor(int dir)
        {
            // menucommon_s *citem = NULL;

            /* see if it's in a valid spot */
            if ((cursor >= 0) && (cursor < items.Count))
            {
                var citem = items[cursor];
                if ((citem.flags & MenuFlags.QMF_INACTIVE) != MenuFlags.QMF_INACTIVE)
                {
                    return;
                }
                // if ((citem = Menu_ItemAtCursor(m)) != 0)
                // {
                //     if (citem->type != MTYPE_SEPARATOR &&
                //         (citem->flags & QMF_INACTIVE) != QMF_INACTIVE)
                //     {
                //         return;
                //     }
                // }
            }

            /* it's not in a valid spot, so crawl in the direction
            indicated until we find a valid spot */
            int count = items.Count;

            while (count-- > 0)
            {
                if (cursor >= 0 && cursor < items.Count)
                {
                    var citem = items[cursor];
                    if ((citem.flags & MenuFlags.QMF_INACTIVE) != MenuFlags.QMF_INACTIVE)
                    {
                        break;
                    }
                }

                // if (citem)
                // {
                    // if (citem->type != MTYPE_SEPARATOR &&
                    // (citem->flags & QMF_INACTIVE) != QMF_INACTIVE)
                    // {
                    //     break;
                    // }
                // }

                cursor += dir;

                if (cursor >= items.Count)
                {
                    cursor = 0;
                }

                if (cursor < 0)
                {
                    cursor = items.Count - 1;
                }
            }
        }        

    }

    internal struct MenuFlags {
        public const uint QMF_LEFT_JUSTIFY        = 0x00000001;
        public const uint QMF_GRAYED              = 0x00000002;
        public const uint QMF_NUMBERSONLY         = 0x00000004;
        public const uint QMF_HIGHLIGHT_IF_FOCUS  = 0x00000008;
        public const uint QMF_INACTIVE            = 0x00000010;

    };

    delegate void QMenuCallback(menucommon_s self);

    internal abstract class menucommon_s
    {
        public string? name { get; init; }
        public int x  { get; init; }
        public int y { get; init; }
        public menuframework_s? parent;
        public int cursor_offset;
        // int localdata[4];
        public uint flags;

        // const char *statusbar;

        public QMenuCallback? callback;
        // void (*callback)(void *self);
        // void (*statusbarfunc)(void *self);
        // void (*ownerdraw)(void *self);
        // void (*cursordraw)(void *self);

        public abstract void Draw(bool focused);

        protected const int RCOLUMN_OFFSET  = 16;
        protected const int LCOLUMN_OFFSET = -16;

        protected void DrawString(int x, int y, string str)
        {
            // unsigned i;
            // float scale = SCR_GetMenuScale();
            float scale = 1.0f;

            for (int i = 0; i < str.Length; i++)
            {
                parent!.client.vid.Draw_CharScaled(x + (int)(i * 8 * scale), (int)(y * scale), str[i], scale);
            }
        }

    }

    internal class menubitmap_s : menucommon_s
    {
        public string? focuspic;	
        public string? errorpic;
        public int     width  { get; init; }
        public int     height  { get; init; }

        public override void Draw(bool focused)
        {
            // float scale = SCR_GetMenuScale();
            float scale = 1.0f;

            if ((flags & MenuFlags.QMF_HIGHLIGHT_IF_FOCUS) != 0 && focused && focuspic != null)
            {
                parent!.client.vid.Draw_PicScaled((int)(x * scale), (int)(y * scale), focuspic, scale);
            }
            else if (name != null)
            {
                 parent!.client.vid.Draw_PicScaled((int)(x * scale), (int)(y * scale), name, scale);
            }

        }
    }

    internal class menuaction_s : menucommon_s
    {
        public override void Draw(bool focused)
        {
            float scale = 1.0f;
            // float scale = SCR_GetMenuScale();
            // int x = 0;
            // int y = 0;

            int x = parent!.x + this.x;
            int y = parent!.y + this.y;

            if ((flags & MenuFlags.QMF_LEFT_JUSTIFY) != 0)
            {
                if ((flags & MenuFlags.QMF_GRAYED) != 0)
                {
            //         Menu_DrawStringDark(x + (LCOLUMN_OFFSET * scale),
            //             y, a->generic.name);
                }
                else
                {
                    DrawString(x + (int)(LCOLUMN_OFFSET * scale), y, name!);
                }
            }
            else
            {
            //     if (a->generic.flags & QMF_GRAYED)
            //     {
            //         Menu_DrawStringR2LDark(x + (LCOLUMN_OFFSET * scale),
            //             y, a->generic.name);
            //     }

            //     else
            //     {
            //         Menu_DrawStringR2L(x + (LCOLUMN_OFFSET * scale),
            //             y, a->generic.name);
            //     }
            }

            // if (a->generic.ownerdraw)
            // {
            //     a->generic.ownerdraw(a);
            // }        
        }
    }

}
