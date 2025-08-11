using System;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Numerics;
using UILayout;

namespace ChartPlayer
{
    struct TargetNote
    {
        public ENoteName Note;
        public int Octave;
        public float Frequency;

        public TargetNote(ENoteName note, int octave)
        {
            this.Note = note;
            this.Octave = octave;

            this.Frequency = (float)NoteUtil.GetMidiNoteFrequency(NoteUtil.GetMidiNoteNumber(note, octave - 1));
        }
    }

    public class TunerInterface : Dock
    {
        static string[] noteNames = new string[]
        {
            "C","C#","D","Eb","E","F","F#","G","Ab","A", "Bb","B"
        };

        int tunerImageWidth = 260;
        int tunerImageHeight = 130;
        float currentPitchCenter;
        TargetNote closestNote = new TargetNote(ENoteName.E, 2);
        float lastPitchCenter = 0;
        float runningCentsOffset = 0;

        int queueSize = 40;
        Queue<float> pitchHistory = new Queue<float>();
        int frameCount = 0;
        DateTime startTime = DateTime.MinValue;
        int lastClosestNote = -1;
        float lastCentsOffset = float.NaN;

        TargetNote[] targetNotes;

        HorizontalStack noteDisplay;
        StringBuilderTextBlock tunerFrequencyDisplay;
        StringBuilderTextBlock tunerCentsDisplay;
        StringBuilderTextBlock tunerNoteDisplay;
        UIColor lineColor;
        EditableImage tunerImage;
        ImageElement tunerImageElement;

        Action<Point> lineDrawAction;
        Action<Point> tunerPointDrawAction;

        public TunerInterface()
            : base()
        {
            lineDrawAction = delegate (Point p)
            {
                tunerImage.SetPixel(p.X, p.Y, lineColor);
            };

            tunerPointDrawAction = delegate (Point p)
            {
                tunerImage.DrawCircle(p.X, p.Y, 1, UIColor.Yellow, fill: true);
            };

            int startNote = NoteUtil.GetMidiNoteNumber(ENoteName.B, 0);
            int endNote = NoteUtil.GetMidiNoteNumber(ENoteName.A, 6);

            targetNotes = new TargetNote[(endNote - startNote) + 1];

            int pos = 0;

            for (int n = startNote; n <= endNote; n++)
            {
                targetNotes[pos++] = new TargetNote(NoteUtil.GetNoteName(n), NoteUtil.GetNoteOctave(n));
            }

            lineColor = UIColor.White;
            lineColor.A = 128;

            var tunerDock = new Dock();
            Children.Add(tunerDock);

            tunerDock.Children.Add(new TextBlock("Tuner") { HorizontalAlignment = EHorizontalAlignment.Center });

            VerticalStack pitchStack = new VerticalStack
            {
                HorizontalAlignment = EHorizontalAlignment.Center,
                VerticalAlignment = EVerticalAlignment.Center,
                BackgroundColor = UIColor.Black
            };
            tunerDock.Children.Add(pitchStack);

            tunerImage = new EditableImage(new UIImage(tunerImageWidth, tunerImageHeight));
            tunerImage.Clear(UIColor.Black);
            tunerImage.UpdateImageData();

            tunerImageElement = new ImageElement(tunerImage.Image)
            {
                DesiredWidth = 260,
                DesiredHeight = 130
            };
            tunerImageElement.Image = tunerImage.Image;
            pitchStack.Children.Add(tunerImageElement);

            noteDisplay = new HorizontalStack
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Bottom,
                Padding = new LayoutPadding(4),
                DesiredHeight = 40,
                ChildSpacing = 4,
                BackgroundColor = lineColor
            };
            pitchStack.Children.Add(noteDisplay);

            tunerFrequencyDisplay = new StringBuilderTextBlock
            {
                //TextFont = Layout.Current.GetFont("SmallFont"),
                TextColor = UIColor.White,
                HorizontalAlignment = EHorizontalAlignment.Center,
                VerticalAlignment = EVerticalAlignment.Center,
            };

            noteDisplay.Children.Add(new UIElementWrapper()
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                DesiredWidth = 100,
                BackgroundColor = UIColor.Black,
                Child = tunerFrequencyDisplay
            });

            tunerNoteDisplay = new StringBuilderTextBlock
            {
                TextColor = UIColor.White,
                HorizontalAlignment = EHorizontalAlignment.Center,
                VerticalAlignment = EVerticalAlignment.Center,
            };

            noteDisplay.Children.Add(new UIElementWrapper()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Stretch,
                BackgroundColor = UIColor.Black,
                Child = tunerNoteDisplay
            });

           tunerCentsDisplay = new StringBuilderTextBlock
           {
               //TextFont = Layout.Current.GetFont("SmallFont"),
               TextColor = UIColor.White,
               HorizontalAlignment = EHorizontalAlignment.Center,
               VerticalAlignment = EVerticalAlignment.Center,
           };

            noteDisplay.Children.Add(new UIElementWrapper()
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                DesiredWidth = 100,
                BackgroundColor = UIColor.Black,
                Child = tunerCentsDisplay
            });
        }

        public void UpdateTuner(float value)
        {
            float newPitch = value;

            if (frameCount == 0)
            {
                DateTime now = DateTime.Now;

                if (startTime != DateTime.MinValue)
                {
                    float elapsedSecs = (float)(now - startTime).TotalSeconds;

                    if (elapsedSecs < 1)
                    {
                        queueSize = (int)((1 / elapsedSecs) * 10);
                    }
                }

                startTime = now;
                frameCount = 10;
            }
            else
            {
                frameCount--;
            }

            tunerImage.Clear(UIColor.Black);

            if (newPitch > 20)
            {
                float diff = float.MaxValue;

                foreach (TargetNote note in targetNotes)
                {
                    float targetDiff = Math.Abs(note.Frequency - newPitch);

                    if (targetDiff > diff)
                        break;

                    diff = targetDiff;

                    closestNote = note;
                }

                currentPitchCenter = closestNote.Frequency;

                float centsOffset = (float)(1200 * Math.Log(newPitch / currentPitchCenter, 2));

                if (!float.IsNaN(lastCentsOffset))
                {
                    centsOffset = (0.1f * centsOffset) + (0.9f * lastCentsOffset);
                }

                lastCentsOffset = centsOffset;

                pitchHistory.Enqueue(centsOffset);

                //Logging.Log("Pitch: " + newPitch + " Closest: " + closestNote.Note.ToString() + closestNote.Octave.ToString() + " " + closestNote.Frequency);

                if (currentPitchCenter != lastPitchCenter)
                {
                    lastPitchCenter = currentPitchCenter;
                }

                int integer = (int)Math.Floor(newPitch);

                tunerFrequencyDisplay.StringBuilder.Clear();
                tunerFrequencyDisplay.StringBuilder.AppendNumber(integer);
                tunerFrequencyDisplay.StringBuilder.Append('.');
                tunerFrequencyDisplay.StringBuilder.AppendNumber((int)Math.Round((newPitch - (float)integer) * 10));
                tunerFrequencyDisplay.StringBuilder.Append("Hz");

                if (Math.Abs(centsOffset - runningCentsOffset) > 10)
                {
                    runningCentsOffset = centsOffset;
                }
                else
                {
                    runningCentsOffset = (runningCentsOffset * .99f) + (centsOffset * .01f);
                }

                tunerCentsDisplay.StringBuilder.Clear();

                int intOffset = (int)Math.Round(runningCentsOffset);

                if (intOffset != 0)
                    tunerCentsDisplay.StringBuilder.Append((runningCentsOffset > 0) ? "+" : "-");

                tunerCentsDisplay.StringBuilder.AppendNumber(Math.Abs(intOffset));
            }
            else
            {
                pitchHistory.Enqueue(float.NaN);

                lastCentsOffset = float.NaN;
            }

            while (pitchHistory.Count > queueSize)
            {
                float val;

                pitchHistory.TryDequeue(out val);
            }

            if (lastClosestNote != (int)closestNote.Note)
            {
                tunerNoteDisplay.StringBuilder.Clear();
                tunerNoteDisplay.StringBuilder.Append(noteNames[(int)closestNote.Note]);

                lastClosestNote = (int)closestNote.Note;
            }

            float closestY = (tunerImageHeight / 2) - 1;

            float step = (float)tunerImageWidth / (float)(queueSize - 1);

            float offset = (step / 2);

            int lastX = -1;
            int lastY = -1;
            float lastOffset = float.NaN;

            foreach (float centsOffset in pitchHistory)
            {
                if (!float.IsNaN(centsOffset))
                {
                    float semitoneOffset = centsOffset / 100;

                    float y = ((float)tunerImageHeight / 2) + (-semitoneOffset * (float)tunerImageHeight);

                    float xOffset = offset;
                    float yOffset = y;

                    if ((yOffset > 0) && (yOffset < tunerImageHeight))
                    {
                        if (!float.IsNaN(lastOffset))
                            tunerImage.DrawLine(new Vector2(lastX, lastY), new Vector2((int)xOffset, (int)Math.Round(yOffset)), tunerPointDrawAction);

                        lastX = (int)xOffset;
                        lastY = (int)yOffset;
                    }
                }

                lastOffset = centsOffset;

                offset += step;
            }

            tunerImage.DrawLine(new Vector2(0, (int)closestY), new Vector2(tunerImage.ImageWidth - 1, (int)closestY), lineDrawAction);

            tunerImage.UpdateImageData();

            noteDisplay.UpdateContentLayout();
        }
    }
}
