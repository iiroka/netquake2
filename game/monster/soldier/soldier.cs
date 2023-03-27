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
 * Soldier aka "Guard". This is the most complex enemy in Quake 2, since
 * it uses all AI features (dodging, sight, crouching, etc) and comes
 * in a myriad of variants.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        private static void soldier_footstep(QuakeGame g, edict_t self)
        {
            // if (!g_monsterfootsteps->value)
            //     return;

            // // Lazy loading for savegame compatibility.
            // if (sound_step == 0 || sound_step2 == 0 || sound_step3 == 0 || sound_step4 == 0)
            // {
            //     sound_step = gi.soundindex("player/step1.wav");
            //     sound_step2 = gi.soundindex("player/step2.wav");
            //     sound_step3 = gi.soundindex("player/step3.wav");
            //     sound_step4 = gi.soundindex("player/step4.wav");
            // }

            // int i;
            // i = randk() % 4;

            // if (i == 0)
            // {
            //     gi.sound(self, CHAN_BODY, sound_step, 1, ATTN_NORM, 0);
            // }
            // else if (i == 1)
            // {
            //     gi.sound(self, CHAN_BODY, sound_step2, 1, ATTN_NORM, 0);
            // }
            // else if (i == 2)
            // {
            //     gi.sound(self, CHAN_BODY, sound_step3, 1, ATTN_NORM, 0);
            // }
            // else if (i == 3)
            // {
            //     gi.sound(self, CHAN_BODY, sound_step4, 1, ATTN_NORM, 0);
            // }
        }

        private static void soldier_idle(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            // if (random() > 0.8)
            // {
            //     gi.sound(self, CHAN_VOICE, sound_idle, 1, ATTN_IDLE, 0);
            // }
        }

        private static void soldier_cock(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            // if (self->s.frame == FRAME_stand322)
            // {
            //     gi.sound(self, CHAN_WEAPON, sound_cock, 1, ATTN_IDLE, 0);
            // }
            // else
            // {
            //     gi.sound(self, CHAN_WEAPON, sound_cock, 1, ATTN_NORM, 0);
            // }
        }


        private static readonly mframe_t[] soldier_frames_stand1 = {
            new mframe_t(ai_stand, 0, soldier_idle),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),

            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),

            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null)
        };

        private static readonly mmove_t soldier_move_stand1 = new mmove_t(
            QuakeGameSoldier.FRAME_stand101,
            QuakeGameSoldier.FRAME_stand130,
            soldier_frames_stand1,
            soldier_stand_f
        );

        private static readonly mframe_t[] soldier_frames_stand3 = {
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),

            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),

            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, soldier_cock),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),

            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null),
            new mframe_t(ai_stand, 0, null)
        };

        private static readonly mmove_t soldier_move_stand3 = new mmove_t(
            QuakeGameSoldier.FRAME_stand301,
            QuakeGameSoldier.FRAME_stand339,
            soldier_frames_stand3,
            soldier_stand_f
        );

        private static void soldier_stand_f(QuakeGame g, edict_t self) {
            g.soldier_stand(self);
        }

        private void soldier_stand(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if ((self.monsterinfo.currentmove == soldier_move_stand3) ||
                (QShared.frandk() < 0.8))
            {
                self.monsterinfo.currentmove = soldier_move_stand1;
            }
            else
            {
                self.monsterinfo.currentmove = soldier_move_stand3;
            }
        }

        private static void soldier_walk1_random(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if (QShared.frandk() > 0.1)
            {
                self.monsterinfo.nextframe = QuakeGameSoldier.FRAME_walk101;
            }
        }

        private static readonly mframe_t[] soldier_frames_walk1 = {
            new mframe_t(ai_walk, 3, null),
            new mframe_t(ai_walk, 6, null),
            new mframe_t(ai_walk, 2, null),
            new mframe_t(ai_walk, 2, soldier_footstep),
            new mframe_t(ai_walk, 2, null),
            new mframe_t(ai_walk, 1, null),
            new mframe_t(ai_walk, 6, null),
            new mframe_t(ai_walk, 5, null),
            new mframe_t(ai_walk, 3, soldier_footstep),
            new mframe_t(ai_walk, -1, soldier_walk1_random),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null),
            new mframe_t(ai_walk, 0, null)
        };

        private static readonly mmove_t soldier_move_walk1 = new mmove_t(
            QuakeGameSoldier.FRAME_walk101,
            QuakeGameSoldier.FRAME_walk133,
            soldier_frames_walk1,
            null
        );

        private static readonly mframe_t[] soldier_frames_walk2 = {
            new mframe_t(ai_walk, 4, soldier_footstep),
            new mframe_t(ai_walk, 4, null),
            new mframe_t(ai_walk, 9, null),
            new mframe_t(ai_walk, 8, null),
            new mframe_t(ai_walk, 5, soldier_footstep),
            new mframe_t(ai_walk, 1, null),
            new mframe_t(ai_walk, 3, null),
            new mframe_t(ai_walk, 7, null),
            new mframe_t(ai_walk, 6, null),
            new mframe_t(ai_walk, 7, null)
        };

        private static readonly mmove_t soldier_move_walk2 = new mmove_t(
            QuakeGameSoldier.FRAME_walk209,
            QuakeGameSoldier.FRAME_walk218,
            soldier_frames_walk2,
            null
        );

        private void soldier_walk(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if (QShared.frandk() < 0.5)
            {
                self.monsterinfo.currentmove = soldier_move_walk1;
            }
            else
            {
                self.monsterinfo.currentmove = soldier_move_walk2;
            }
        }


        private static readonly mframe_t[] soldier_frames_start_run = {
            new mframe_t(ai_run, 7, null),
            new mframe_t(ai_run, 5, null)
        };

        private static readonly mmove_t soldier_move_start_run = new mmove_t(
            QuakeGameSoldier.FRAME_run01,
            QuakeGameSoldier.FRAME_run02,
            soldier_frames_start_run,
            soldier_run_f
        );

        private static readonly mframe_t[]  soldier_frames_run = {
            new mframe_t(ai_run, 10, null),
            new mframe_t(ai_run, 11, soldier_footstep),
            new mframe_t(ai_run, 11, null),
            new mframe_t(ai_run, 16, null),
            new mframe_t(ai_run, 10, soldier_footstep),
            new mframe_t(ai_run, 15, null)
        };

        private static readonly mmove_t soldier_move_run = new mmove_t(
            QuakeGameSoldier.FRAME_run03,
            QuakeGameSoldier.FRAME_run08,
            soldier_frames_run,
            null
        );

        private void soldier_run(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if ((self.monsterinfo.aiflags & AI_STAND_GROUND) != 0)
            {
                self.monsterinfo.currentmove = soldier_move_stand1;
                return;
            }

            if ((self.monsterinfo.currentmove == soldier_move_walk1) ||
                (self.monsterinfo.currentmove == soldier_move_walk2) ||
                (self.monsterinfo.currentmove == soldier_move_start_run))
            {
                self.monsterinfo.currentmove = soldier_move_run;
            }
            else
            {
                self.monsterinfo.currentmove = soldier_move_start_run;
            }
        }

        private static void soldier_run_f(QuakeGame g, edict_t self)
        {
            g.soldier_run(self);
        }

        private static readonly mframe_t[] soldier_frames_pain1 = {
            new mframe_t(ai_move, -3, null),
            new mframe_t(ai_move, 4, null),
            new mframe_t(ai_move, 1, null),
            new mframe_t(ai_move, 1, null),
            new mframe_t(ai_move, 0, null)
        };

        private static readonly mmove_t soldier_move_pain1 = new mmove_t(
            QuakeGameSoldier.FRAME_pain101,
            QuakeGameSoldier.FRAME_pain105,
            soldier_frames_pain1,
            soldier_run_f
        );

        private static readonly mframe_t[] soldier_frames_pain2 = {
            new mframe_t(ai_move, -13, null),
            new mframe_t(ai_move, -1, null),
            new mframe_t(ai_move, 2, null),
            new mframe_t(ai_move, 4, null),
            new mframe_t(ai_move, 2, null),
            new mframe_t(ai_move, 3, null),
            new mframe_t(ai_move, 2, null)
        };

        private static readonly mmove_t soldier_move_pain2 = new mmove_t(
            QuakeGameSoldier.FRAME_pain201,
            QuakeGameSoldier.FRAME_pain207,
            soldier_frames_pain2,
            soldier_run_f
        );

        private static readonly mframe_t[] soldier_frames_pain3 = {
            new mframe_t(ai_move, -8, null),
            new mframe_t(ai_move, 10, null),
            new mframe_t(ai_move, -4, soldier_footstep),
            new mframe_t(ai_move, -1, null),
            new mframe_t(ai_move, -3, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 3, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 1, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 1, null),
            new mframe_t(ai_move, 2, null),
            new mframe_t(ai_move, 4, null),
            new mframe_t(ai_move, 3, null),
            new mframe_t(ai_move, 2, soldier_footstep)
        };

        private static readonly mmove_t soldier_move_pain3 = new mmove_t(
            QuakeGameSoldier.FRAME_pain301,
            QuakeGameSoldier.FRAME_pain318,
            soldier_frames_pain3,
            soldier_run_f
        );

        private static readonly mframe_t[] soldier_frames_pain4 = {
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, -10, null),
            new mframe_t(ai_move, -6, null),
            new mframe_t(ai_move, 8, null),
            new mframe_t(ai_move, 4, null),
            new mframe_t(ai_move, 1, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 2, null),
            new mframe_t(ai_move, 5, null),
            new mframe_t(ai_move, 2, null),
            new mframe_t(ai_move, -1, null),
            new mframe_t(ai_move, -1, null),
            new mframe_t(ai_move, 3, null),
            new mframe_t(ai_move, 2, null),
            new mframe_t(ai_move, 0, null)
        };

        private static readonly mmove_t soldier_move_pain4 = new mmove_t(
            QuakeGameSoldier.FRAME_pain401,
            QuakeGameSoldier.FRAME_pain417,
            soldier_frames_pain4,
            soldier_run_f
        );

        private void soldier_pain(edict_t self, edict_t _other /* unused */,
                float _kick /* unused */, int _damage /* unused */)
        {
            if (self == null)
            {
                return;
            }

            if (self.health < (self.max_health / 2))
            {
                self.s.skinnum |= 1;
            }

            if (level.time < self.pain_debounce_time)
            {
                if ((self.velocity[2] > 100) &&
                    ((self.monsterinfo.currentmove == soldier_move_pain1) ||
                    (self.monsterinfo.currentmove == soldier_move_pain2) ||
                    (self.monsterinfo.currentmove == soldier_move_pain3)))
                {
                    self.monsterinfo.currentmove = soldier_move_pain4;
                }

                return;
            }

            self.pain_debounce_time = level.time + 3;

            // var n = self.s.skinnum | 1;

            // if (n == 1)
            // {
            //     gi.sound(self, CHAN_VOICE, sound_pain_light, 1, ATTN_NORM, 0);
            // }
            // else if (n == 3)
            // {
            //     gi.sound(self, CHAN_VOICE, sound_pain, 1, ATTN_NORM, 0);
            // }
            // else
            // {
            //     gi.sound(self, CHAN_VOICE, sound_pain_ss, 1, ATTN_NORM, 0);
            // }

            if (self.velocity[2] > 100)
            {
                self.monsterinfo.currentmove = soldier_move_pain4;
                return;
            }

            if (skill!.Int == SKILL_HARDPLUS)
            {
                return; /* no pain anims in nightmare */
            }

            var r = QShared.frandk();

            if (r < 0.33)
            {
                self.monsterinfo.currentmove = soldier_move_pain1;
            }
            else if (r < 0.66)
            {
                self.monsterinfo.currentmove = soldier_move_pain2;
            }
            else
            {
                self.monsterinfo.currentmove = soldier_move_pain3;
            }
        }

        private static readonly int[] blaster_flash =
        {
            QShared.MZ2_SOLDIER_BLASTER_1,
            QShared.MZ2_SOLDIER_BLASTER_2,
            QShared.MZ2_SOLDIER_BLASTER_3,
            QShared.MZ2_SOLDIER_BLASTER_4,
            QShared.MZ2_SOLDIER_BLASTER_5,
            QShared.MZ2_SOLDIER_BLASTER_6,
            QShared.MZ2_SOLDIER_BLASTER_7,
            QShared.MZ2_SOLDIER_BLASTER_8
        };

        private static readonly int[] shotgun_flash =
        {
            QShared.MZ2_SOLDIER_SHOTGUN_1,
            QShared.MZ2_SOLDIER_SHOTGUN_2,
            QShared.MZ2_SOLDIER_SHOTGUN_3,
            QShared.MZ2_SOLDIER_SHOTGUN_4,
            QShared.MZ2_SOLDIER_SHOTGUN_5,
            QShared.MZ2_SOLDIER_SHOTGUN_6,
            QShared.MZ2_SOLDIER_SHOTGUN_7,
            QShared.MZ2_SOLDIER_SHOTGUN_8
        };

        private static readonly int[] machinegun_flash =
        {
            QShared.MZ2_SOLDIER_MACHINEGUN_1,
            QShared.MZ2_SOLDIER_MACHINEGUN_2,
            QShared.MZ2_SOLDIER_MACHINEGUN_3,
            QShared.MZ2_SOLDIER_MACHINEGUN_4,
            QShared.MZ2_SOLDIER_MACHINEGUN_5,
            QShared.MZ2_SOLDIER_MACHINEGUN_6,
            QShared.MZ2_SOLDIER_MACHINEGUN_7,
            QShared.MZ2_SOLDIER_MACHINEGUN_8
        };

        private void soldier_fire(edict_t self, int flash_number)
        {
            // vec3_t start;
            // vec3_t forward, right, up;
            // vec3_t aim;
            // vec3_t dir;
            // vec3_t end;
            // float r, u;
            // int flash_index;

            if (self == null)
            {
                return;
            }

            int flash_index;
            if (self.s.skinnum < 2)
            {
                flash_index = blaster_flash[flash_number];
            }
            else if (self.s.skinnum < 4)
            {
                flash_index = shotgun_flash[flash_number];
            }
            else
            {
                flash_index = machinegun_flash[flash_number];
            }

            var forward = new Vector3();
            var right = new Vector3();
            var _up = new Vector3();
            QShared.AngleVectors(self.s.angles, ref forward, ref right, ref _up);
            var start = new Vector3();
            G_ProjectSource(self.s.origin, QShared.monster_flash_offset[flash_index],
                    forward, right, ref start);

            var aim = new Vector3();
            if ((flash_number == 5) || (flash_number == 6))
            {
                aim = forward;
            }
            else
            {
                var end = self.enemy!.s.origin;
                end[2] += self.enemy.viewheight;
                aim = end - start;
                vectoangles(aim, out var dir);
                var up = new Vector3();
                QShared.AngleVectors(dir, ref forward, ref right, ref up);

                var r = QShared.crandk() * 1000;
                var u = QShared.crandk() * 500;
                QShared.VectorMA(start, 8192, forward, out end);
                QShared.VectorMA(end, r, right, out end);
                QShared.VectorMA(end, u, up, out end);

                aim = end - start;
                aim = Vector3.Normalize(aim);
            }

            if (self.s.skinnum <= 1)
            {
                // monster_fire_blaster(self, start, aim, 5, 600, flash_index, EF_BLASTER);
            }
            else if (self.s.skinnum <= 3)
            {
                // monster_fire_shotgun(self, start, aim, 2, 1,
                //         DEFAULT_SHOTGUN_HSPREAD, DEFAULT_SHOTGUN_VSPREAD,
                //         DEFAULT_SHOTGUN_COUNT, flash_index);
            }
            else
            {
                if ((self.monsterinfo.aiflags & AI_HOLD_FRAME) == 0)
                {
                    self.monsterinfo.pausetime = level.time + (3 + QShared.randk() % 8) * FRAMETIME;
                }

                // monster_fire_bullet(self, start, aim, 2, 4,
                //         DEFAULT_BULLET_HSPREAD, DEFAULT_BULLET_VSPREAD,
                //         flash_index);

                if (level.time >= self.monsterinfo.pausetime)
                {
                    self.monsterinfo.aiflags &= ~AI_HOLD_FRAME;
                }
                else
                {
                    self.monsterinfo.aiflags |= AI_HOLD_FRAME;
                }
            }
        }

        /* ATTACK1 (blaster/shotgun) */
        private static void soldier_fire1(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            g.soldier_fire(self, 0);
        }

        private static void soldier_attack1_refire1(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if (self.s.skinnum > 1)
            {
                return;
            }

            if (self.enemy!.health <= 0)
            {
                return;
            }

            if (((g.skill!.Int == SKILL_HARDPLUS) &&
                (QShared.frandk() < 0.5)) || (g.range(self, self.enemy) == RANGE_MELEE))
            {
                self.monsterinfo.nextframe = QuakeGameSoldier.FRAME_attak102;
            }
            else
            {
                self.monsterinfo.nextframe = QuakeGameSoldier.FRAME_attak110;
            }
        }

        private static void soldier_attack1_refire2(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if (self.s.skinnum < 2)
            {
                return;
            }

            if (self.enemy!.health <= 0)
            {
                return;
            }

            if (((g.skill!.Int == SKILL_HARDPLUS) &&
                (QShared.frandk() < 0.5)) || (g.range(self, self.enemy) == RANGE_MELEE))
            {
                self.monsterinfo.nextframe = QuakeGameSoldier.FRAME_attak102;
            }
        }

        private static readonly mframe_t[] soldier_frames_attack1 = {
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_fire1),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_attack1_refire1),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_cock),
            new mframe_t(ai_charge, 0, soldier_attack1_refire2),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null)
        };

        private static readonly mmove_t soldier_move_attack1 = new mmove_t(
            QuakeGameSoldier.FRAME_attak101,
            QuakeGameSoldier.FRAME_attak112,
            soldier_frames_attack1,
            soldier_run_f
        );

        /* ATTACK2 (blaster/shotgun) */
        private static void soldier_fire2(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            g.soldier_fire(self, 1);
        }

        private static void soldier_attack2_refire1(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if (self.s.skinnum > 1)
            {
                return;
            }

            if (self.enemy!.health <= 0)
            {
                return;
            }

            if (((g.skill!.Int == SKILL_HARDPLUS) &&
                (QShared.frandk() < 0.5)) || (g.range(self, self.enemy) == RANGE_MELEE))
            {
                self.monsterinfo.nextframe = QuakeGameSoldier.FRAME_attak204;
            }
            else
            {
                self.monsterinfo.nextframe = QuakeGameSoldier.FRAME_attak216;
            }
        }

        private static void soldier_attack2_refire2(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if (self.s.skinnum < 2)
            {
                return;
            }

            if (self.enemy!.health <= 0)
            {
                return;
            }

            if (((g.skill!.Int == SKILL_HARDPLUS) &&
                (QShared.frandk() < 0.5)) || (g.range(self, self.enemy) == RANGE_MELEE))
            {
                self.monsterinfo.nextframe = QuakeGameSoldier.FRAME_attak204;
            }
        }

        private static readonly mframe_t[] soldier_frames_attack2 = {
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_fire2),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_attack2_refire1),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_cock),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_attack2_refire2),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null)
        };

        private static readonly mmove_t soldier_move_attack2 = new mmove_t(
            QuakeGameSoldier.FRAME_attak201,
            QuakeGameSoldier.FRAME_attak218,
            soldier_frames_attack2,
            soldier_run_f
        );

        /* ATTACK3 (duck and shoot) */
        private static void soldier_duck_down(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if ((self.monsterinfo.aiflags & AI_DUCKED) != 0)
            {
                return;
            }

            self.monsterinfo.aiflags |= AI_DUCKED;
            self.maxs[2] -= 32;
            self.takedamage = (int)damage_t.DAMAGE_YES;
            self.monsterinfo.pausetime = g.level.time + 1;
            g.gi.linkentity(self);
        }

        private static void soldier_duck_up(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            self.monsterinfo.aiflags &= ~AI_DUCKED;
            self.maxs[2] += 32;
            self.takedamage = (int)damage_t.DAMAGE_AIM;
            g.gi.linkentity(self);
        }

        private static void soldier_fire3(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            soldier_duck_down(g, self);
            g.soldier_fire(self, 2);
        }

        private static void soldier_attack3_refire(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if ((g.level.time + 0.4) < self.monsterinfo.pausetime)
            {
                self.monsterinfo.nextframe = QuakeGameSoldier.FRAME_attak303;
            }
        }

        private static readonly mframe_t[] soldier_frames_attack3 = {
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_fire3),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_attack3_refire),
            new mframe_t(ai_charge, 0, soldier_duck_up),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null)
        };

        private static readonly mmove_t soldier_move_attack3 = new mmove_t(
            QuakeGameSoldier.FRAME_attak301,
            QuakeGameSoldier.FRAME_attak309,
            soldier_frames_attack3,
            soldier_run_f
        );

        /* ATTACK4 (machinegun) */
        private static void soldier_fire4(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            g.soldier_fire(self, 3);
        }

        private static readonly mframe_t[] soldier_frames_attack4 = {
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_footstep),
            new mframe_t(ai_charge, 0, soldier_fire4),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, null),
            new mframe_t(ai_charge, 0, soldier_footstep)
        };

        private static readonly mmove_t soldier_move_attack4 = new mmove_t(
            QuakeGameSoldier.FRAME_attak401,
            QuakeGameSoldier.FRAME_attak406,
            soldier_frames_attack4,
            soldier_run_f
        );

        /* ATTACK6 (run & shoot) */
        private static void soldier_fire8(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            g.soldier_fire(self, 7);
        }

        private static void soldier_attack6_refire(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if (self.enemy!.health <= 0)
            {
                return;
            }

            if (g.range(self, self.enemy) < RANGE_MID)
            {
                return;
            }

            if (g.skill!.Int == SKILL_HARDPLUS)
            {
                self.monsterinfo.nextframe = QuakeGameSoldier.FRAME_runs03;
            }
        }

        private static readonly mframe_t[] soldier_frames_attack6 = {
            new mframe_t(ai_charge, 10, null),
            new mframe_t(ai_charge, 4, null),
            new mframe_t(ai_charge, 12, soldier_footstep),
            new mframe_t(ai_charge, 11, soldier_fire8),
            new mframe_t(ai_charge, 13, null),
            new mframe_t(ai_charge, 18, null),
            new mframe_t(ai_charge, 15, soldier_footstep),
            new mframe_t(ai_charge, 14, null),
            new mframe_t(ai_charge, 11, null),
            new mframe_t(ai_charge, 8, soldier_footstep),
            new mframe_t(ai_charge, 11, null),
            new mframe_t(ai_charge, 12, null),
            new mframe_t(ai_charge, 12, soldier_footstep),
            new mframe_t(ai_charge, 17, soldier_attack6_refire)
        };

        private static readonly mmove_t soldier_move_attack6 = new mmove_t(
            QuakeGameSoldier.FRAME_runs01,
            QuakeGameSoldier.FRAME_runs14,
            soldier_frames_attack6,
            soldier_run_f
        );

        private void soldier_attack(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if (self.s.skinnum < 4)
            {
                if (QShared.frandk() < 0.5)
                {
                    self.monsterinfo.currentmove = soldier_move_attack1;
                }
                else
                {
                    self.monsterinfo.currentmove = soldier_move_attack2;
                }
            }
            else
            {
                self.monsterinfo.currentmove = soldier_move_attack4;
            }
        }

        private static void soldier_fire6(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            g.soldier_fire(self, 5);
        }

        private static void soldier_fire7(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            g.soldier_fire(self, 6);
        }

        private static void soldier_dead(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            self.mins = new Vector3(-16, -16, -24);
            self.maxs = new Vector3( 16, 16, -8);
            self.movetype = movetype_t.MOVETYPE_TOSS;
            self.svflags |= QGameFlags.SVF_DEADMONSTER;
            self.nextthink = 0;
            g.gi.linkentity(self);
        }

        private static mframe_t[] soldier_frames_death1 = {
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, -10, null),
            new mframe_t(ai_move, -10, null),
            new mframe_t(ai_move, -10, null),
            new mframe_t(ai_move, -5, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, soldier_fire6),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, soldier_fire7),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null)
        };

        private static mmove_t soldier_move_death1 = new mmove_t(
            QuakeGameSoldier.FRAME_death101,
            QuakeGameSoldier.FRAME_death136,
            soldier_frames_death1,
            soldier_dead
        );

        private static mframe_t[] soldier_frames_death2 = {
            new mframe_t(ai_move, -5, null),
            new mframe_t(ai_move, -5, null),
            new mframe_t(ai_move, -5, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null)
        };

        private static mmove_t soldier_move_death2 = new mmove_t(
            QuakeGameSoldier.FRAME_death201,
            QuakeGameSoldier.FRAME_death235,
            soldier_frames_death2,
            soldier_dead
        );

        private static mframe_t[] soldier_frames_death3 = {
            new mframe_t(ai_move, -5, null),
            new mframe_t(ai_move, -5, null),
            new mframe_t(ai_move, -5, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
        };

        private static mmove_t soldier_move_death3 = new mmove_t(
            QuakeGameSoldier.FRAME_death301,
            QuakeGameSoldier.FRAME_death345,
            soldier_frames_death3,
            soldier_dead
        );

        private static mframe_t[] soldier_frames_death4 = {
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null)
        };

        private static mmove_t soldier_move_death4 = new mmove_t(
            QuakeGameSoldier.FRAME_death401,
            QuakeGameSoldier.FRAME_death453,
            soldier_frames_death4,
            soldier_dead
        );

        private static mframe_t[] soldier_frames_death5 = {
            new mframe_t(ai_move, -5, null),
            new mframe_t(ai_move, -5, null),
            new mframe_t(ai_move, -5, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),

            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null)
        };

        private static mmove_t soldier_move_death5 = new mmove_t(
            QuakeGameSoldier.FRAME_death501,
            QuakeGameSoldier.FRAME_death524,
            soldier_frames_death5,
            soldier_dead
        );

        private static mframe_t[] soldier_frames_death6 = {
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null),
            new mframe_t(ai_move, 0, null)
        };

        private static mmove_t soldier_move_death6 = new mmove_t(
            QuakeGameSoldier.FRAME_death601,
            QuakeGameSoldier.FRAME_death610,
            soldier_frames_death6,
            soldier_dead
        );

        private void soldier_die(edict_t self, edict_t _inflictor /* unused */,
                edict_t _attacker /* unused */, int damage,
                in Vector3 point)
        {

            /* check for gib */
            if (self.health <= self.gib_health)
            {
                // gi.sound(self, CHAN_VOICE, gi.soundindex("misc/udeath.wav"), 1, ATTN_NORM, 0);

                // for (n = 0; n < 3; n++)
                // {
                //     ThrowGib(self, "models/objects/gibs/sm_meat/tris.md2",
                //             damage, GIB_ORGANIC);
                // }

                // ThrowGib(self, "models/objects/gibs/chest/tris.md2",
                //         damage, GIB_ORGANIC);
                // ThrowHead(self, "models/objects/gibs/head2/tris.md2",
                //         damage, GIB_ORGANIC);
                self.deadflag = DEAD_DEAD;
                return;
            }

            if (self.deadflag == DEAD_DEAD)
            {
                return;
            }

            /* regular death */
            self.deadflag = DEAD_DEAD;
            self.takedamage = (int)damage_t.DAMAGE_YES;
            self.s.skinnum |= 1;

            // if (self.s.skinnum == 1)
            // {
            //     gi.sound(self, CHAN_VOICE, sound_death_light, 1, ATTN_NORM, 0);
            // }
            // else if (self->s.skinnum == 3)
            // {
            //     gi.sound(self, CHAN_VOICE, sound_death, 1, ATTN_NORM, 0);
            // }
            // else
            // {
            //     gi.sound(self, CHAN_VOICE, sound_death_ss, 1, ATTN_NORM, 0);
            // }

            if (MathF.Abs((self.s.origin[2] + self.viewheight) - point[2]) <= 4)
            {
                /* head shot */
                self.monsterinfo.currentmove = soldier_move_death3;
                return;
            }

            var n = QShared.randk() % 5;

            if (n == 0)
            {
                self.monsterinfo.currentmove = soldier_move_death1;
            }
            else if (n == 1)
            {
                self.monsterinfo.currentmove = soldier_move_death2;
            }
            else if (n == 2)
            {
                self.monsterinfo.currentmove = soldier_move_death4;
            }
            else if (n == 3)
            {
                self.monsterinfo.currentmove = soldier_move_death5;
            }
            else
            {
                self.monsterinfo.currentmove = soldier_move_death6;
            }
        }


        private void SP_monster_soldier_x(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            // Force recaching at next footstep to ensure
            // that the sound indices are correct.
            // sound_step = 0;
            // sound_step2 = 0;
            // sound_step3 = 0;
            // sound_step4 = 0;

            self.s.modelindex = gi.modelindex("models/monsters/soldier/tris.md2");
            self.monsterinfo.scale = QuakeGameSoldier.MODEL_SCALE;
            self.mins = new Vector3(-16, -16, -24);
            self.maxs = new Vector3(16, 16, 32);
            self.movetype = movetype_t.MOVETYPE_STEP;
            self.solid = solid_t.SOLID_BBOX;

            // sound_idle = gi.soundindex("soldier/solidle1.wav");
            // sound_sight1 = gi.soundindex("soldier/solsght1.wav");
            // sound_sight2 = gi.soundindex("soldier/solsrch1.wav");
            // sound_cock = gi.soundindex("infantry/infatck3.wav");

            self.mass = 100;

            self.pain = soldier_pain;
            self.die = soldier_die;

            self.monsterinfo.stand = soldier_stand;
            self.monsterinfo.walk = soldier_walk;
            self.monsterinfo.run = soldier_run;
            // self->monsterinfo.dodge = soldier_dodge;
            self.monsterinfo.attack = soldier_attack;
            // self->monsterinfo.melee = NULL;
            // self->monsterinfo.sight = soldier_sight;

            gi.linkentity(self);

            self.monsterinfo.stand(self);

            walkmonster_start(self);
        }

        /*
        * QUAKED monster_soldier_light (1 .5 0) (-16 -16 -24) (16 16 32) Ambush Trigger_Spawn Sight
        */
        private static void SP_monster_soldier_light(QuakeGame g, edict_t? self)
        {
            if (self == null)
            {
                return;
            }

            if (g.deathmatch!.Bool)
            {
                g.G_FreeEdict(self);
                return;
            }

            self.health = 20;
            self.gib_health = -30;

            g.SP_monster_soldier_x(self);

            // sound_pain_light = gi.soundindex("soldier/solpain2.wav");
            // sound_death_light = gi.soundindex("soldier/soldeth2.wav");
            // sound_step = gi.soundindex("player/step1.wav");
            // sound_step2 = gi.soundindex("player/step2.wav");
            // sound_step3 = gi.soundindex("player/step3.wav");
            // sound_step4 = gi.soundindex("player/step4.wav");
            // gi.modelindex("models/objects/laser/tris.md2");
            // gi.soundindex("misc/lasfly.wav");
            // gi.soundindex("soldier/solatck2.wav");

            self.s.skinnum = 0;
        }

        /*
        * QUAKED monster_soldier (1 .5 0) (-16 -16 -24) (16 16 32) Ambush Trigger_Spawn Sight
        */
        private static void SP_monster_soldier(QuakeGame g, edict_t? self)
        {
            if (self == null)
            {
                return;
            }

            if (g.deathmatch!.Bool)
            {
                g.G_FreeEdict(self);
                return;
            }

            self.health = 30;
            self.gib_health = -30;

            g.SP_monster_soldier_x(self);

            // sound_pain = gi.soundindex("soldier/solpain1.wav");
            // sound_death = gi.soundindex("soldier/soldeth1.wav");
            // sound_step = gi.soundindex("player/step1.wav");
            // sound_step2 = gi.soundindex("player/step2.wav");
            // sound_step3 = gi.soundindex("player/step3.wav");
            // sound_step4 = gi.soundindex("player/step4.wav");
            // gi.soundindex("soldier/solatck1.wav");

            self.s.skinnum = 2;
        }
    }

    class QuakeGameSoldier
    {

        public const int FRAME_attak101 = 0;
        public const int FRAME_attak102 = 1;
        public const int FRAME_attak103 = 2;
        public const int FRAME_attak104 = 3;
        public const int FRAME_attak105 = 4;
        public const int FRAME_attak106 = 5;
        public const int FRAME_attak107 = 6;
        public const int FRAME_attak108 = 7;
        public const int FRAME_attak109 = 8;
        public const int FRAME_attak110 = 9;
        public const int FRAME_attak111 = 10;
        public const int FRAME_attak112 = 11;
        public const int FRAME_attak201 = 12;
        public const int FRAME_attak202 = 13;
        public const int FRAME_attak203 = 14;
        public const int FRAME_attak204 = 15;
        public const int FRAME_attak205 = 16;
        public const int FRAME_attak206 = 17;
        public const int FRAME_attak207 = 18;
        public const int FRAME_attak208 = 19;
        public const int FRAME_attak209 = 20;
        public const int FRAME_attak210 = 21;
        public const int FRAME_attak211 = 22;
        public const int FRAME_attak212 = 23;
        public const int FRAME_attak213 = 24;
        public const int FRAME_attak214 = 25;
        public const int FRAME_attak215 = 26;
        public const int FRAME_attak216 = 27;
        public const int FRAME_attak217 = 28;
        public const int FRAME_attak218 = 29;
        public const int FRAME_attak301 = 30;
        public const int FRAME_attak302 = 31;
        public const int FRAME_attak303 = 32;
        public const int FRAME_attak304 = 33;
        public const int FRAME_attak305 = 34;
        public const int FRAME_attak306 = 35;
        public const int FRAME_attak307 = 36;
        public const int FRAME_attak308 = 37;
        public const int FRAME_attak309 = 38;
        public const int FRAME_attak401 = 39;
        public const int FRAME_attak402 = 40;
        public const int FRAME_attak403 = 41;
        public const int FRAME_attak404 = 42;
        public const int FRAME_attak405 = 43;
        public const int FRAME_attak406 = 44;
        public const int FRAME_duck01 = 45;
        public const int FRAME_duck02 = 46;
        public const int FRAME_duck03 = 47;
        public const int FRAME_duck04 = 48;
        public const int FRAME_duck05 = 49;
        public const int FRAME_pain101 = 50;
        public const int FRAME_pain102 = 51;
        public const int FRAME_pain103 = 52;
        public const int FRAME_pain104 = 53;
        public const int FRAME_pain105 = 54;
        public const int FRAME_pain201 = 55;
        public const int FRAME_pain202 = 56;
        public const int FRAME_pain203 = 57;
        public const int FRAME_pain204 = 58;
        public const int FRAME_pain205 = 59;
        public const int FRAME_pain206 = 60;
        public const int FRAME_pain207 = 61;
        public const int FRAME_pain301 = 62;
        public const int FRAME_pain302 = 63;
        public const int FRAME_pain303 = 64;
        public const int FRAME_pain304 = 65;
        public const int FRAME_pain305 = 66;
        public const int FRAME_pain306 = 67;
        public const int FRAME_pain307 = 68;
        public const int FRAME_pain308 = 69;
        public const int FRAME_pain309 = 70;
        public const int FRAME_pain310 = 71;
        public const int FRAME_pain311 = 72;
        public const int FRAME_pain312 = 73;
        public const int FRAME_pain313 = 74;
        public const int FRAME_pain314 = 75;
        public const int FRAME_pain315 = 76;
        public const int FRAME_pain316 = 77;
        public const int FRAME_pain317 = 78;
        public const int FRAME_pain318 = 79;
        public const int FRAME_pain401 = 80;
        public const int FRAME_pain402 = 81;
        public const int FRAME_pain403 = 82;
        public const int FRAME_pain404 = 83;
        public const int FRAME_pain405 = 84;
        public const int FRAME_pain406 = 85;
        public const int FRAME_pain407 = 86;
        public const int FRAME_pain408 = 87;
        public const int FRAME_pain409 = 88;
        public const int FRAME_pain410 = 89;
        public const int FRAME_pain411 = 90;
        public const int FRAME_pain412 = 91;
        public const int FRAME_pain413 = 92;
        public const int FRAME_pain414 = 93;
        public const int FRAME_pain415 = 94;
        public const int FRAME_pain416 = 95;
        public const int FRAME_pain417 = 96;
        public const int FRAME_run01 = 97;
        public const int FRAME_run02 = 98;
        public const int FRAME_run03 = 99;
        public const int FRAME_run04 = 100;
        public const int FRAME_run05 = 101;
        public const int FRAME_run06 = 102;
        public const int FRAME_run07 = 103;
        public const int FRAME_run08 = 104;
        public const int FRAME_run09 = 105;
        public const int FRAME_run10 = 106;
        public const int FRAME_run11 = 107;
        public const int FRAME_run12 = 108;
        public const int FRAME_runs01 = 109;
        public const int FRAME_runs02 = 110;
        public const int FRAME_runs03 = 111;
        public const int FRAME_runs04 = 112;
        public const int FRAME_runs05 = 113;
        public const int FRAME_runs06 = 114;
        public const int FRAME_runs07 = 115;
        public const int FRAME_runs08 = 116;
        public const int FRAME_runs09 = 117;
        public const int FRAME_runs10 = 118;
        public const int FRAME_runs11 = 119;
        public const int FRAME_runs12 = 120;
        public const int FRAME_runs13 = 121;
        public const int FRAME_runs14 = 122;
        public const int FRAME_runs15 = 123;
        public const int FRAME_runs16 = 124;
        public const int FRAME_runs17 = 125;
        public const int FRAME_runs18 = 126;
        public const int FRAME_runt01 = 127;
        public const int FRAME_runt02 = 128;
        public const int FRAME_runt03 = 129;
        public const int FRAME_runt04 = 130;
        public const int FRAME_runt05 = 131;
        public const int FRAME_runt06 = 132;
        public const int FRAME_runt07 = 133;
        public const int FRAME_runt08 = 134;
        public const int FRAME_runt09 = 135;
        public const int FRAME_runt10 = 136;
        public const int FRAME_runt11 = 137;
        public const int FRAME_runt12 = 138;
        public const int FRAME_runt13 = 139;
        public const int FRAME_runt14 = 140;
        public const int FRAME_runt15 = 141;
        public const int FRAME_runt16 = 142;
        public const int FRAME_runt17 = 143;
        public const int FRAME_runt18 = 144;
        public const int FRAME_runt19 = 145;
        public const int FRAME_stand101 = 146;
        public const int FRAME_stand102 = 147;
        public const int FRAME_stand103 = 148;
        public const int FRAME_stand104 = 149;
        public const int FRAME_stand105 = 150;
        public const int FRAME_stand106 = 151;
        public const int FRAME_stand107 = 152;
        public const int FRAME_stand108 = 153;
        public const int FRAME_stand109 = 154;
        public const int FRAME_stand110 = 155;
        public const int FRAME_stand111 = 156;
        public const int FRAME_stand112 = 157;
        public const int FRAME_stand113 = 158;
        public const int FRAME_stand114 = 159;
        public const int FRAME_stand115 = 160;
        public const int FRAME_stand116 = 161;
        public const int FRAME_stand117 = 162;
        public const int FRAME_stand118 = 163;
        public const int FRAME_stand119 = 164;
        public const int FRAME_stand120 = 165;
        public const int FRAME_stand121 = 166;
        public const int FRAME_stand122 = 167;
        public const int FRAME_stand123 = 168;
        public const int FRAME_stand124 = 169;
        public const int FRAME_stand125 = 170;
        public const int FRAME_stand126 = 171;
        public const int FRAME_stand127 = 172;
        public const int FRAME_stand128 = 173;
        public const int FRAME_stand129 = 174;
        public const int FRAME_stand130 = 175;
        public const int FRAME_stand301 = 176;
        public const int FRAME_stand302 = 177;
        public const int FRAME_stand303 = 178;
        public const int FRAME_stand304 = 179;
        public const int FRAME_stand305 = 180;
        public const int FRAME_stand306 = 181;
        public const int FRAME_stand307 = 182;
        public const int FRAME_stand308 = 183;
        public const int FRAME_stand309 = 184;
        public const int FRAME_stand310 = 185;
        public const int FRAME_stand311 = 186;
        public const int FRAME_stand312 = 187;
        public const int FRAME_stand313 = 188;
        public const int FRAME_stand314 = 189;
        public const int FRAME_stand315 = 190;
        public const int FRAME_stand316 = 191;
        public const int FRAME_stand317 = 192;
        public const int FRAME_stand318 = 193;
        public const int FRAME_stand319 = 194;
        public const int FRAME_stand320 = 195;
        public const int FRAME_stand321 = 196;
        public const int FRAME_stand322 = 197;
        public const int FRAME_stand323 = 198;
        public const int FRAME_stand324 = 199;
        public const int FRAME_stand325 = 200;
        public const int FRAME_stand326 = 201;
        public const int FRAME_stand327 = 202;
        public const int FRAME_stand328 = 203;
        public const int FRAME_stand329 = 204;
        public const int FRAME_stand330 = 205;
        public const int FRAME_stand331 = 206;
        public const int FRAME_stand332 = 207;
        public const int FRAME_stand333 = 208;
        public const int FRAME_stand334 = 209;
        public const int FRAME_stand335 = 210;
        public const int FRAME_stand336 = 211;
        public const int FRAME_stand337 = 212;
        public const int FRAME_stand338 = 213;
        public const int FRAME_stand339 = 214;
        public const int FRAME_walk101 = 215;
        public const int FRAME_walk102 = 216;
        public const int FRAME_walk103 = 217;
        public const int FRAME_walk104 = 218;
        public const int FRAME_walk105 = 219;
        public const int FRAME_walk106 = 220;
        public const int FRAME_walk107 = 221;
        public const int FRAME_walk108 = 222;
        public const int FRAME_walk109 = 223;
        public const int FRAME_walk110 = 224;
        public const int FRAME_walk111 = 225;
        public const int FRAME_walk112 = 226;
        public const int FRAME_walk113 = 227;
        public const int FRAME_walk114 = 228;
        public const int FRAME_walk115 = 229;
        public const int FRAME_walk116 = 230;
        public const int FRAME_walk117 = 231;
        public const int FRAME_walk118 = 232;
        public const int FRAME_walk119 = 233;
        public const int FRAME_walk120 = 234;
        public const int FRAME_walk121 = 235;
        public const int FRAME_walk122 = 236;
        public const int FRAME_walk123 = 237;
        public const int FRAME_walk124 = 238;
        public const int FRAME_walk125 = 239;
        public const int FRAME_walk126 = 240;
        public const int FRAME_walk127 = 241;
        public const int FRAME_walk128 = 242;
        public const int FRAME_walk129 = 243;
        public const int FRAME_walk130 = 244;
        public const int FRAME_walk131 = 245;
        public const int FRAME_walk132 = 246;
        public const int FRAME_walk133 = 247;
        public const int FRAME_walk201 = 248;
        public const int FRAME_walk202 = 249;
        public const int FRAME_walk203 = 250;
        public const int FRAME_walk204 = 251;
        public const int FRAME_walk205 = 252;
        public const int FRAME_walk206 = 253;
        public const int FRAME_walk207 = 254;
        public const int FRAME_walk208 = 255;
        public const int FRAME_walk209 = 256;
        public const int FRAME_walk210 = 257;
        public const int FRAME_walk211 = 258;
        public const int FRAME_walk212 = 259;
        public const int FRAME_walk213 = 260;
        public const int FRAME_walk214 = 261;
        public const int FRAME_walk215 = 262;
        public const int FRAME_walk216 = 263;
        public const int FRAME_walk217 = 264;
        public const int FRAME_walk218 = 265;
        public const int FRAME_walk219 = 266;
        public const int FRAME_walk220 = 267;
        public const int FRAME_walk221 = 268;
        public const int FRAME_walk222 = 269;
        public const int FRAME_walk223 = 270;
        public const int FRAME_walk224 = 271;
        public const int FRAME_death101 = 272;
        public const int FRAME_death102 = 273;
        public const int FRAME_death103 = 274;
        public const int FRAME_death104 = 275;
        public const int FRAME_death105 = 276;
        public const int FRAME_death106 = 277;
        public const int FRAME_death107 = 278;
        public const int FRAME_death108 = 279;
        public const int FRAME_death109 = 280;
        public const int FRAME_death110 = 281;
        public const int FRAME_death111 = 282;
        public const int FRAME_death112 = 283;
        public const int FRAME_death113 = 284;
        public const int FRAME_death114 = 285;
        public const int FRAME_death115 = 286;
        public const int FRAME_death116 = 287;
        public const int FRAME_death117 = 288;
        public const int FRAME_death118 = 289;
        public const int FRAME_death119 = 290;
        public const int FRAME_death120 = 291;
        public const int FRAME_death121 = 292;
        public const int FRAME_death122 = 293;
        public const int FRAME_death123 = 294;
        public const int FRAME_death124 = 295;
        public const int FRAME_death125 = 296;
        public const int FRAME_death126 = 297;
        public const int FRAME_death127 = 298;
        public const int FRAME_death128 = 299;
        public const int FRAME_death129 = 300;
        public const int FRAME_death130 = 301;
        public const int FRAME_death131 = 302;
        public const int FRAME_death132 = 303;
        public const int FRAME_death133 = 304;
        public const int FRAME_death134 = 305;
        public const int FRAME_death135 = 306;
        public const int FRAME_death136 = 307;
        public const int FRAME_death201 = 308;
        public const int FRAME_death202 = 309;
        public const int FRAME_death203 = 310;
        public const int FRAME_death204 = 311;
        public const int FRAME_death205 = 312;
        public const int FRAME_death206 = 313;
        public const int FRAME_death207 = 314;
        public const int FRAME_death208 = 315;
        public const int FRAME_death209 = 316;
        public const int FRAME_death210 = 317;
        public const int FRAME_death211 = 318;
        public const int FRAME_death212 = 319;
        public const int FRAME_death213 = 320;
        public const int FRAME_death214 = 321;
        public const int FRAME_death215 = 322;
        public const int FRAME_death216 = 323;
        public const int FRAME_death217 = 324;
        public const int FRAME_death218 = 325;
        public const int FRAME_death219 = 326;
        public const int FRAME_death220 = 327;
        public const int FRAME_death221 = 328;
        public const int FRAME_death222 = 329;
        public const int FRAME_death223 = 330;
        public const int FRAME_death224 = 331;
        public const int FRAME_death225 = 332;
        public const int FRAME_death226 = 333;
        public const int FRAME_death227 = 334;
        public const int FRAME_death228 = 335;
        public const int FRAME_death229 = 336;
        public const int FRAME_death230 = 337;
        public const int FRAME_death231 = 338;
        public const int FRAME_death232 = 339;
        public const int FRAME_death233 = 340;
        public const int FRAME_death234 = 341;
        public const int FRAME_death235 = 342;
        public const int FRAME_death301 = 343;
        public const int FRAME_death302 = 344;
        public const int FRAME_death303 = 345;
        public const int FRAME_death304 = 346;
        public const int FRAME_death305 = 347;
        public const int FRAME_death306 = 348;
        public const int FRAME_death307 = 349;
        public const int FRAME_death308 = 350;
        public const int FRAME_death309 = 351;
        public const int FRAME_death310 = 352;
        public const int FRAME_death311 = 353;
        public const int FRAME_death312 = 354;
        public const int FRAME_death313 = 355;
        public const int FRAME_death314 = 356;
        public const int FRAME_death315 = 357;
        public const int FRAME_death316 = 358;
        public const int FRAME_death317 = 359;
        public const int FRAME_death318 = 360;
        public const int FRAME_death319 = 361;
        public const int FRAME_death320 = 362;
        public const int FRAME_death321 = 363;
        public const int FRAME_death322 = 364;
        public const int FRAME_death323 = 365;
        public const int FRAME_death324 = 366;
        public const int FRAME_death325 = 367;
        public const int FRAME_death326 = 368;
        public const int FRAME_death327 = 369;
        public const int FRAME_death328 = 370;
        public const int FRAME_death329 = 371;
        public const int FRAME_death330 = 372;
        public const int FRAME_death331 = 373;
        public const int FRAME_death332 = 374;
        public const int FRAME_death333 = 375;
        public const int FRAME_death334 = 376;
        public const int FRAME_death335 = 377;
        public const int FRAME_death336 = 378;
        public const int FRAME_death337 = 379;
        public const int FRAME_death338 = 380;
        public const int FRAME_death339 = 381;
        public const int FRAME_death340 = 382;
        public const int FRAME_death341 = 383;
        public const int FRAME_death342 = 384;
        public const int FRAME_death343 = 385;
        public const int FRAME_death344 = 386;
        public const int FRAME_death345 = 387;
        public const int FRAME_death401 = 388;
        public const int FRAME_death402 = 389;
        public const int FRAME_death403 = 390;
        public const int FRAME_death404 = 391;
        public const int FRAME_death405 = 392;
        public const int FRAME_death406 = 393;
        public const int FRAME_death407 = 394;
        public const int FRAME_death408 = 395;
        public const int FRAME_death409 = 396;
        public const int FRAME_death410 = 397;
        public const int FRAME_death411 = 398;
        public const int FRAME_death412 = 399;
        public const int FRAME_death413 = 400;
        public const int FRAME_death414 = 401;
        public const int FRAME_death415 = 402;
        public const int FRAME_death416 = 403;
        public const int FRAME_death417 = 404;
        public const int FRAME_death418 = 405;
        public const int FRAME_death419 = 406;
        public const int FRAME_death420 = 407;
        public const int FRAME_death421 = 408;
        public const int FRAME_death422 = 409;
        public const int FRAME_death423 = 410;
        public const int FRAME_death424 = 411;
        public const int FRAME_death425 = 412;
        public const int FRAME_death426 = 413;
        public const int FRAME_death427 = 414;
        public const int FRAME_death428 = 415;
        public const int FRAME_death429 = 416;
        public const int FRAME_death430 = 417;
        public const int FRAME_death431 = 418;
        public const int FRAME_death432 = 419;
        public const int FRAME_death433 = 420;
        public const int FRAME_death434 = 421;
        public const int FRAME_death435 = 422;
        public const int FRAME_death436 = 423;
        public const int FRAME_death437 = 424;
        public const int FRAME_death438 = 425;
        public const int FRAME_death439 = 426;
        public const int FRAME_death440 = 427;
        public const int FRAME_death441 = 428;
        public const int FRAME_death442 = 429;
        public const int FRAME_death443 = 430;
        public const int FRAME_death444 = 431;
        public const int FRAME_death445 = 432;
        public const int FRAME_death446 = 433;
        public const int FRAME_death447 = 434;
        public const int FRAME_death448 = 435;
        public const int FRAME_death449 = 436;
        public const int FRAME_death450 = 437;
        public const int FRAME_death451 = 438;
        public const int FRAME_death452 = 439;
        public const int FRAME_death453 = 440;
        public const int FRAME_death501 = 441;
        public const int FRAME_death502 = 442;
        public const int FRAME_death503 = 443;
        public const int FRAME_death504 = 444;
        public const int FRAME_death505 = 445;
        public const int FRAME_death506 = 446;
        public const int FRAME_death507 = 447;
        public const int FRAME_death508 = 448;
        public const int FRAME_death509 = 449;
        public const int FRAME_death510 = 450;
        public const int FRAME_death511 = 451;
        public const int FRAME_death512 = 452;
        public const int FRAME_death513 = 453;
        public const int FRAME_death514 = 454;
        public const int FRAME_death515 = 455;
        public const int FRAME_death516 = 456;
        public const int FRAME_death517 = 457;
        public const int FRAME_death518 = 458;
        public const int FRAME_death519 = 459;
        public const int FRAME_death520 = 460;
        public const int FRAME_death521 = 461;
        public const int FRAME_death522 = 462;
        public const int FRAME_death523 = 463;
        public const int FRAME_death524 = 464;
        public const int FRAME_death601 = 465;
        public const int FRAME_death602 = 466;
        public const int FRAME_death603 = 467;
        public const int FRAME_death604 = 468;
        public const int FRAME_death605 = 469;
        public const int FRAME_death606 = 470;
        public const int FRAME_death607 = 471;
        public const int FRAME_death608 = 472;
        public const int FRAME_death609 = 473;
        public const int FRAME_death610 = 474;

        public const float MODEL_SCALE = 1.200000f;

    }
}