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
 * Player (the arm and the weapons) animation.
 *
 * =======================================================================
 */
namespace Quake2 {

    class QuakeGamePlayer
    {
        public const int  FRAME_stand01 = 0;
        public const int  FRAME_stand02 = 1;
        public const int  FRAME_stand03 = 2;
        public const int  FRAME_stand04 = 3;
        public const int  FRAME_stand05 = 4;
        public const int  FRAME_stand06 = 5;
        public const int  FRAME_stand07 = 6;
        public const int  FRAME_stand08 = 7;
        public const int  FRAME_stand09 = 8;
        public const int  FRAME_stand10 = 9;
        public const int  FRAME_stand11 = 10;
        public const int  FRAME_stand12 = 11;
        public const int  FRAME_stand13 = 12;
        public const int  FRAME_stand14 = 13;
        public const int  FRAME_stand15 = 14;
        public const int  FRAME_stand16 = 15;
        public const int  FRAME_stand17 = 16;
        public const int  FRAME_stand18 = 17;
        public const int  FRAME_stand19 = 18;
        public const int  FRAME_stand20 = 19;
        public const int  FRAME_stand21 = 20;
        public const int  FRAME_stand22 = 21;
        public const int  FRAME_stand23 = 22;
        public const int  FRAME_stand24 = 23;
        public const int  FRAME_stand25 = 24;
        public const int  FRAME_stand26 = 25;
        public const int  FRAME_stand27 = 26;
        public const int  FRAME_stand28 = 27;
        public const int  FRAME_stand29 = 28;
        public const int  FRAME_stand30 = 29;
        public const int  FRAME_stand31 = 30;
        public const int  FRAME_stand32 = 31;
        public const int  FRAME_stand33 = 32;
        public const int  FRAME_stand34 = 33;
        public const int  FRAME_stand35 = 34;
        public const int  FRAME_stand36 = 35;
        public const int  FRAME_stand37 = 36;
        public const int  FRAME_stand38 = 37;
        public const int  FRAME_stand39 = 38;
        public const int  FRAME_stand40 = 39;
        public const int  FRAME_run1 = 40;
        public const int  FRAME_run2 = 41;
        public const int  FRAME_run3 = 42;
        public const int  FRAME_run4 = 43;
        public const int  FRAME_run5 = 44;
        public const int  FRAME_run6 = 45;
        public const int  FRAME_attack1 = 46;
        public const int  FRAME_attack2 = 47;
        public const int  FRAME_attack3 = 48;
        public const int  FRAME_attack4 = 49;
        public const int  FRAME_attack5 = 50;
        public const int  FRAME_attack6 = 51;
        public const int  FRAME_attack7 = 52;
        public const int  FRAME_attack8 = 53;
        public const int  FRAME_pain101 = 54;
        public const int  FRAME_pain102 = 55;
        public const int  FRAME_pain103 = 56;
        public const int  FRAME_pain104 = 57;
        public const int  FRAME_pain201 = 58;
        public const int  FRAME_pain202 = 59;
        public const int  FRAME_pain203 = 60;
        public const int  FRAME_pain204 = 61;
        public const int  FRAME_pain301 = 62;
        public const int  FRAME_pain302 = 63;
        public const int  FRAME_pain303 = 64;
        public const int  FRAME_pain304 = 65;
        public const int  FRAME_jump1 = 66;
        public const int  FRAME_jump2 = 67;
        public const int  FRAME_jump3 = 68;
        public const int  FRAME_jump4 = 69;
        public const int  FRAME_jump5 = 70;
        public const int  FRAME_jump6 = 71;
        public const int  FRAME_flip01 = 72;
        public const int  FRAME_flip02 = 73;
        public const int  FRAME_flip03 = 74;
        public const int  FRAME_flip04 = 75;
        public const int  FRAME_flip05 = 76;
        public const int  FRAME_flip06 = 77;
        public const int  FRAME_flip07 = 78;
        public const int  FRAME_flip08 = 79;
        public const int  FRAME_flip09 = 80;
        public const int  FRAME_flip10 = 81;
        public const int  FRAME_flip11 = 82;
        public const int  FRAME_flip12 = 83;
        public const int  FRAME_salute01 = 84;
        public const int  FRAME_salute02 = 85;
        public const int  FRAME_salute03 = 86;
        public const int  FRAME_salute04 = 87;
        public const int  FRAME_salute05 = 88;
        public const int  FRAME_salute06 = 89;
        public const int  FRAME_salute07 = 90;
        public const int  FRAME_salute08 = 91;
        public const int  FRAME_salute09 = 92;
        public const int  FRAME_salute10 = 93;
        public const int  FRAME_salute11 = 94;
        public const int  FRAME_taunt01 = 95;
        public const int  FRAME_taunt02 = 96;
        public const int  FRAME_taunt03 = 97;
        public const int  FRAME_taunt04 = 98;
        public const int  FRAME_taunt05 = 99;
        public const int  FRAME_taunt06 = 100;
        public const int  FRAME_taunt07 = 101;
        public const int  FRAME_taunt08 = 102;
        public const int  FRAME_taunt09 = 103;
        public const int  FRAME_taunt10 = 104;
        public const int  FRAME_taunt11 = 105;
        public const int  FRAME_taunt12 = 106;
        public const int  FRAME_taunt13 = 107;
        public const int  FRAME_taunt14 = 108;
        public const int  FRAME_taunt15 = 109;
        public const int  FRAME_taunt16 = 110;
        public const int  FRAME_taunt17 = 111;
        public const int  FRAME_wave01 = 112;
        public const int  FRAME_wave02 = 113;
        public const int  FRAME_wave03 = 114;
        public const int  FRAME_wave04 = 115;
        public const int  FRAME_wave05 = 116;
        public const int  FRAME_wave06 = 117;
        public const int  FRAME_wave07 = 118;
        public const int  FRAME_wave08 = 119;
        public const int  FRAME_wave09 = 120;
        public const int  FRAME_wave10 = 121;
        public const int  FRAME_wave11 = 122;
        public const int  FRAME_point01 = 123;
        public const int  FRAME_point02 = 124;
        public const int  FRAME_point03 = 125;
        public const int  FRAME_point04 = 126;
        public const int  FRAME_point05 = 127;
        public const int  FRAME_point06 = 128;
        public const int  FRAME_point07 = 129;
        public const int  FRAME_point08 = 130;
        public const int  FRAME_point09 = 131;
        public const int  FRAME_point10 = 132;
        public const int  FRAME_point11 = 133;
        public const int  FRAME_point12 = 134;
        public const int  FRAME_crstnd01 = 135;
        public const int  FRAME_crstnd02 = 136;
        public const int  FRAME_crstnd03 = 137;
        public const int  FRAME_crstnd04 = 138;
        public const int  FRAME_crstnd05 = 139;
        public const int  FRAME_crstnd06 = 140;
        public const int  FRAME_crstnd07 = 141;
        public const int  FRAME_crstnd08 = 142;
        public const int  FRAME_crstnd09 = 143;
        public const int  FRAME_crstnd10 = 144;
        public const int  FRAME_crstnd11 = 145;
        public const int  FRAME_crstnd12 = 146;
        public const int  FRAME_crstnd13 = 147;
        public const int  FRAME_crstnd14 = 148;
        public const int  FRAME_crstnd15 = 149;
        public const int  FRAME_crstnd16 = 150;
        public const int  FRAME_crstnd17 = 151;
        public const int  FRAME_crstnd18 = 152;
        public const int  FRAME_crstnd19 = 153;
        public const int  FRAME_crwalk1 = 154;
        public const int  FRAME_crwalk2 = 155;
        public const int  FRAME_crwalk3 = 156;
        public const int  FRAME_crwalk4 = 157;
        public const int  FRAME_crwalk5 = 158;
        public const int  FRAME_crwalk6 = 159;
        public const int  FRAME_crattak1 = 160;
        public const int  FRAME_crattak2 = 161;
        public const int  FRAME_crattak3 = 162;
        public const int  FRAME_crattak4 = 163;
        public const int  FRAME_crattak5 = 164;
        public const int  FRAME_crattak6 = 165;
        public const int  FRAME_crattak7 = 166;
        public const int  FRAME_crattak8 = 167;
        public const int  FRAME_crattak9 = 168;
        public const int  FRAME_crpain1 = 169;
        public const int  FRAME_crpain2 = 170;
        public const int  FRAME_crpain3 = 171;
        public const int  FRAME_crpain4 = 172;
        public const int  FRAME_crdeath1 = 173;
        public const int  FRAME_crdeath2 = 174;
        public const int  FRAME_crdeath3 = 175;
        public const int  FRAME_crdeath4 = 176;
        public const int  FRAME_crdeath5 = 177;
        public const int  FRAME_death101 = 178;
        public const int  FRAME_death102 = 179;
        public const int  FRAME_death103 = 180;
        public const int  FRAME_death104 = 181;
        public const int  FRAME_death105 = 182;
        public const int  FRAME_death106 = 183;
        public const int  FRAME_death201 = 184;
        public const int  FRAME_death202 = 185;
        public const int  FRAME_death203 = 186;
        public const int  FRAME_death204 = 187;
        public const int  FRAME_death205 = 188;
        public const int  FRAME_death206 = 189;
        public const int  FRAME_death301 = 190;
        public const int  FRAME_death302 = 191;
        public const int  FRAME_death303 = 192;
        public const int  FRAME_death304 = 193;
        public const int  FRAME_death305 = 194;
        public const int  FRAME_death306 = 195;
        public const int  FRAME_death307 = 196;
        public const int  FRAME_death308 = 197;

        public const double MODEL_SCALE = 1.000000;

    }
}