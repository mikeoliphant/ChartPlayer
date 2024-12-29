using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using UILayout;
using SongFormat;
using AudioPlugSharp;

namespace ChartPlayer
{
    public class DrumPlayerScene3D : ChartScene3D, IMidiHandler
    {
        static int[] ScaleWhiteBlack = { 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0 };
        static float[] ScaleOffsets = { 0, 0.5f, 1, 1.5f, 2, 3, 3.5f, 4, 4.5f, 5, 5.5f, 6 };

        int numLanes = 5;
        float targetCameraDistance = 64;
        float cameraDistance = 70;
        float positionLane;

        public DrumPlayerScene3D(SongPlayer player)
            : base(player)
        {
            ChartPlayerGame.Instance.Plugin.MidiHandler = this;

            positionLane = (float)numLanes / 2;

            highwayStartX = 0;
            highwayEndX = GetLanePosition(numLanes);
        }

        public void HandleNoteOn(int channel, int noteNumber, float velocity, int sampleOffset)
        {
            DrumHit? hit = DrumMidiDeviceConfiguration.CurrentMap.HandleNoteOn(channel, noteNumber, velocity, sampleOffset, isLive: true);

            if (hit != null)
            {
                HandleHit(hit.Value);
            }
        }

        public void HandlePolyPressure(int channel, int noteNumber, float pressure, int sampleOffset)
        {
            DrumHit? hit = DrumMidiDeviceConfiguration.CurrentMap.HandlePolyPressure(channel, noteNumber, pressure, sampleOffset, isLive: true);

            if (hit != null)
            {
                HandleHit(hit.Value);
            }
        }

        void HandleHit(DrumHit hit)
        {

        }

        public override void Draw()
        {
            base.Draw();

            if (ChartPlayerGame.Instance.Plugin.SongPlayer != null)
            {
                currentTime = (float)player.CurrentSecond;

                targetCameraDistance = 85;

                float targetPositionKey = (float)numLanes / 2;

                positionLane = MathUtil.Lerp(positionLane, targetPositionKey, 0.01f);

                cameraDistance = MathUtil.Lerp(cameraDistance, targetCameraDistance, 0.01f);

                Camera.Position = new Vector3(GetLanePosition(positionLane), 70, -(float)(currentTime * timeScale) + cameraDistance);
                Camera.SetLookAt(new Vector3(GetLanePosition(positionLane), 0, Camera.Position.Z - (NoteDisplaySeconds * timeScale) * .3f));
            }
        }

        public override void DrawQuads()
        {
            base.DrawQuads();

            FogEnabled = true;
            FogStart = 400;
            FogEnd = cameraDistance + (NoteDisplaySeconds * timeScale);
            FogColor = UIColor.Black;

            try
            {
                if (player != null)
                {
                    for (int lane = 0; lane <= numLanes; lane++)
                    {
                        DrawLaneTimeLine(lane, 0, startTime, endTime, whiteHalfAlpha);
                    }

                    var notes = player.SongDrumNotes.Notes.Where(n => n.TimeOffset >= startTime).OrderByDescending(n => n.TimeOffset);

                    // Draw the notes
                    foreach (SongDrumNote note in notes)
                    {
                        if (note.KitPiece == EDrumKitPiece.Kick)
                        {
                            DrawLaneHorizontalLine(0.25f, numLanes - 0.25f, note.TimeOffset, 0, UIColor.Yellow, .1f);
                        }
                        else
                        {
                            string imageName = null;
                            int drawLane = 0;
                            float drawLaneOffset;
                            bool haveCrash2 = true;

                            EDrumArticulation articulation = note.Articulation;

                            if (articulation == EDrumArticulation.None)
                                articulation = SongDrumNote.GetDefaultArticulation(note.KitPiece);

                            switch (note.KitPiece)
                            {
                                case EDrumKitPiece.Snare:
                                    switch (articulation)
                                    {
                                        case EDrumArticulation.DrumHead:
                                            imageName = "DrumRed";
                                            drawLane = 0;
                                            break;
                                        case EDrumArticulation.DrumRim:
                                            imageName = "DrumRedStick";
                                            drawLane = 0;
                                            break;
                                        case EDrumArticulation.SideStick:
                                            imageName = "DrumRedStick";
                                            drawLane = 0;
                                            break;
                                    }
                                    break;
                                case EDrumKitPiece.HiHat:
                                    switch (articulation)
                                    {
                                        case EDrumArticulation.HiHatClosed:
                                            imageName = "CymbalYellow";
                                            drawLane = 1;
                                            break;
                                        case EDrumArticulation.HiHatOpen:
                                            imageName = "CymbalYellowOpen";
                                            drawLane = 1;
                                            break;
                                        case EDrumArticulation.HiHatChick:
                                            imageName = "CymbalYellowFoot";
                                            drawLane = 1;
                                            break;
                                    }
                                    break;
                                case EDrumKitPiece.Crash:
                                    imageName = "CymbalGreen";
                                    drawLane = 2;
                                    break;

                                case EDrumKitPiece.Crash2:
                                    imageName = "CymbalGreen";

                                    if (haveCrash2)
                                    {
                                        drawLane = 4;
                                    }
                                    else
                                    {
                                        drawLane = 2;
                                        drawLaneOffset = 5;
                                    }
                                    break;

                                case EDrumKitPiece.Ride:
                                    if (articulation == EDrumArticulation.CymbalBell)
                                    {
                                        imageName = "CymbalBlueBell";
                                    }
                                    else if (articulation == EDrumArticulation.CymbalBow)
                                    {
                                        imageName = "CymbalBlue";
                                    }
                                    drawLane = 3;
                                    break;
                                case EDrumKitPiece.Tom1:
                                    imageName = "DrumYellow";
                                    drawLane = 1;
                                    break;
                                case EDrumKitPiece.Tom2:
                                    imageName = "DrumGreen";
                                    drawLane = 2;
                                    break;
                                case EDrumKitPiece.Tom3:
                                    imageName = "DrumBlue";
                                    drawLane = 3;
                                    break;
                            }

                            if (articulation == EDrumArticulation.CymbalChoke)
                            {
                                imageName = "CymbalChoke";
                            }

                            if (imageName != null)
                                DrawVerticalImage(Layout.Current.GetImage(imageName), drawLane + 0.5f, note.TimeOffset, 0, UIColor.White, .08f);
                        }
                    }

                    DrawLaneHorizontalLine(0, numLanes, startTime, 0, UIColor.White, .04f);
                }
            }
            catch (Exception ex)
            {
                Layout.Current.ShowContinuePopup("Draw error: \n\n" + ex.ToString());
            }
        }

        float GetLanePosition(float lane)
        {
            return lane * 15;
        }

        void DrawLaneTimeLine(float keyCenter, float height, float startTime, float endTime, UIColor color)
        {
            keyCenter = GetLanePosition(keyCenter);
            startTime *= -timeScale;
            endTime *= -timeScale;

            UIImage image = Layout.Current.GetImage("VerticalFretLine");

            float imageScale = 0.03f;

            float minX = (float)keyCenter - ((float)image.Width * imageScale);
            float maxX = (float)keyCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, height, startTime), color, new Vector3(minX, height, endTime), color, new Vector3(maxX, height, endTime), color, new Vector3(maxX, height, startTime), color);
        }

        void DrawLaneHorizontalLine(float startLane, float endLane, float time, float heightOffset, UIColor color, float imageScale)
        {
            startLane = GetLanePosition(startLane);
            endLane = GetLanePosition(endLane);
            time *= -timeScale;

            UIImage image = Layout.Current.GetImage("HorizontalFretLine");

            float minZ = time + ((float)image.Height * imageScale);
            float maxZ = time - ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startLane, heightOffset, minZ), color, new Vector3(startLane, heightOffset, maxZ), color, new Vector3(endLane, heightOffset, maxZ), color, new Vector3(endLane, heightOffset, minZ), color);
        }

        void DrawVerticalImage(UIImage image, float laneCenter, float timeCenter, float heightOffset, in UIColor color, float imageScale)
        {
            laneCenter = GetLanePosition(laneCenter);
            timeCenter *= -timeScale;

            float minX = laneCenter - ((float)image.Width * imageScale);
            float maxX = laneCenter + ((float)image.Width * imageScale);

            float minY = heightOffset - ((float)image.Height * imageScale);
            float maxY = heightOffset + ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(minX, minY, timeCenter), color, new Vector3(minX, maxY, timeCenter), color, new Vector3(maxX, maxY, timeCenter), color, new Vector3(maxX, minY, timeCenter), color);
        }

        void DrawVerticalImage(UIImage image, float startLane, float endLane, float time, float heightOffset, in UIColor color, float imageScale)
        {
            startLane = GetLanePosition(startLane);
            endLane = GetLanePosition(endLane);
            time *= -timeScale;

            float minY = heightOffset - ((float)image.Height * imageScale);
            float maxY = heightOffset + ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startLane, minY, time), color, new Vector3(startLane, maxY, time), color, new Vector3(endLane, maxY, time), color, new Vector3(endLane, minY, time), color);
        }


        void DrawFlatImage(UIImage image, float laneCenter, float timeOffset, float heightOffset, UIColor color, float imageScale)
        {
            laneCenter = GetLanePosition(laneCenter);
            timeOffset *= -timeScale;

            float minX = laneCenter - ((float)image.Width * imageScale);
            float maxX = laneCenter + ((float)image.Width * imageScale);

            float minZ = timeOffset - ((float)image.Height * imageScale);
            float maxZ = timeOffset + ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(minX, heightOffset, minZ), color, new Vector3(minX, heightOffset, maxZ), color, new Vector3(maxX, heightOffset, maxZ), color, new Vector3(maxX, heightOffset, minZ), color);
        }
    }
}
