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
 * This file implements all generic particle stuff
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QClient {

        private class cparticle_t {

            public cparticle_t? next;

            public float		time;

            public Vector3		org;
            public Vector3		vel;
            public Vector3		accel;
            public float		color;
            public float		colorvel;
            public float		alpha;
            public float		alphavel;
        }

        private cparticle_t? free_particles = null;
        private cparticle_t? active_particles = null;
        private cparticle_t[] particles;
        private int cl_numparticles = QRef.MAX_PARTICLES;

        private void CL_ClearParticles()
        {
            int i;

            free_particles = particles[0];
            active_particles = null;

            for (i = 0; i < cl_numparticles-1; i++)
            {
                particles[i].next = particles[i + 1];
            }

            particles[cl_numparticles - 1].next = null;
        }

        private void CL_ParticleEffect(in Vector3 org, in Vector3 dir, int color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (free_particles == null)
                {
                    return;
                }

                var p = free_particles;
                free_particles = p.next;
                p.next = active_particles;
                active_particles = p;

                p.time = cl.time;
                p.color = color + (QShared.randk() & 7);
                var d = (float)(QShared.randk() & 31);

                p.org.X = org.X + ((QShared.randk() & 7) - 4) + d * dir.X;
                p.org.Y = org.Y + ((QShared.randk() & 7) - 4) + d * dir.Y;
                p.org.Z = org.Z + ((QShared.randk() & 7) - 4) + d * dir.Z;
                p.vel.X = QShared.crandk() * 20;
                p.vel.Y = QShared.crandk() * 20;
                p.vel.Z = QShared.crandk() * 20;

                p.accel.X = 0;
                p.accel.Y = 0;
                p.accel.Z = -PARTICLE_GRAVITY + 0.2f;
                p.alpha = 1.0f;

                p.alphavel = -1.0f / (0.5f + QShared.frandk() * 0.3f);
            }
        }

        private void CL_AddParticles()
        {
            float alpha;
            float time, time2;

            cparticle_t? active = null;
            cparticle_t? tail = null;
            cparticle_t? p = null;
            cparticle_t? next = null;

            for (p = active_particles; p != null; p = next)
            {
                next = p.next;

                if (p.alphavel != INSTANT_PARTICLE)
                {
                    time = (cl.time - p.time) * 0.001f;
                    alpha = p.alpha + time * p.alphavel;

                    if (alpha <= 0)
                    {
                        /* faded out */
                        p.next = free_particles;
                        free_particles = p;
                        continue;
                    }
                }
                else
                {
                    time = 0.0f;
                    alpha = p.alpha;
                }

                p.next = null;

                if (tail == null)
                {
                    active = tail = p;
                }

                else
                {
                    tail.next = p;
                    tail = p;
                }

                if (alpha > 1.0f)
                {
                    alpha = 1;
                }

                var color = p.color;
                time2 = time * time;

                Vector3 org = p.org + p.vel * time + p.accel * time2;

                V_AddParticle(org, (uint)color, alpha);

                if (p.alphavel == INSTANT_PARTICLE)
                {
                    p.alphavel = 0;
                    p.alpha = 0;
                }
            }

            active_particles = active;
        }

    }
}