using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using UILayout;
using SongFormat;

namespace ChartPlayer
{
    public class FretCamera : Camera3D
    {
        public float CameraDistance { get; private set; } = 70;
        public float FocusDist { get; set; } = 600;

        float targetCameraDistance = 75;
        float positionFret = 3;

        public FretCamera()
        {
            Position = new Vector3(0, 0, 5);
            Up = Vector3.Up;
            Forward = new Vector3(0, 0, -1);

            FieldOfView = (float)Math.PI / 4.0f;
        }

        public void Update(float minFret, float maxFret, float targetFocusFret, float focusY)
        {
            if (minFret <= maxFret)
            {
                float fretDist = maxFret - minFret;

                fretDist -= 12;

                if (fretDist < 0)
                    fretDist = 0;

                targetCameraDistance = 65 + (Math.Max(fretDist, 4) * 3);

                float targetPositionFret = ((float)maxFret + (float)minFret) / 2;

                if (targetPositionFret < (targetFocusFret - 3))
                {
                    targetPositionFret = targetFocusFret - 3;
                }

                if (targetPositionFret > (targetFocusFret + 5))
                {
                    targetPositionFret = targetFocusFret + 5;
                }

                positionFret = MathUtil.Lerp(positionFret, MathUtil.Clamp(targetPositionFret, 3.5f, 24) - 1, 0.02f);
            }

            CameraDistance = MathUtil.Lerp(CameraDistance, targetCameraDistance, 0.01f);

            float fretOffset = (10 - positionFret) / 4.0f;

            Position = new Vector3(FretPlayerScene3D.GetFretPosition(positionFret + fretOffset), 50, focusY + CameraDistance);
            SetLookAt(new Vector3(FretPlayerScene3D.GetFretPosition(positionFret), 0, Position.Z - (FocusDist * .3f)));
        }
    }

    public class FretPlayerScene3D : ChartScene3D
    {
        static UIColor[] stringColors = new UIColor[] { UIColor.Red, UIColor.Yellow, new UIColor(0, .6f, 1.0f, 1.0f), UIColor.Orange, new UIColor(.1f, .8f, 0), new UIColor(.8f, 0, .8f) };
        static string[] stringColorNames = { "Red", "Yellow", "Cyan", "Orange", "Green", "Purple" };

        public bool DisplayNotes { get; set; } = true;
        public float DetectSemitoneOffset { get; set; } = 0;
        public NoteDetector NoteDetector { get; private set; }

        float targetFocusFret = 2;
        int numFrets = 24;
        float minFret = 0;
        float maxFret = 4;
        SongNote? firstNote;
        int numStrings;
        bool isDetected = false;

        FretCamera fretCamera;

        Dictionary<float, bool> nonRepeatChords = new Dictionary<float, bool>();
        Dictionary<float, bool> nonRepeatNotes = new Dictionary<float, bool>();

        string[] numberStrings;
        UIImage[] stringNoteImages;
        UIImage[] stringNoteTrailImages;

        double[] stringOffsetSemitones;

        Thread noteDetectThread;

        sbyte[] notesDetected;
        SongNote? currentChordNote;
        SongNote? currentFingerNote;
        bool currentChordDetected;
        SongNote?[] currentStringNotes;
        bool[] currentStringDetected;
        int[] currentStringFingers;
        int startNotePosition = 0;

        public FretPlayerScene3D(SongPlayer player)
            : base(player)
        {
            Camera = fretCamera = new FretCamera();

            numberStrings = new string[numFrets + 1];
            for (int i = 0; i < numberStrings.Length; i++)
            {
                numberStrings[i] = i.ToString();
            }

            highwayStartX = GetFretPosition(0);
            highwayEndX = GetFretPosition(numFrets);

            stringNoteImages = new UIImage[6];
            stringNoteTrailImages = new UIImage[6];

            for (int strng = 0; strng < 6; strng++)
            {
                stringNoteImages[strng] = Layout.Current.GetImage("Guitar" + stringColorNames[strng]);
                stringNoteTrailImages[strng] = Layout.Current.GetImage("NoteTrail" + stringColorNames[strng]);
            }

            numStrings = (player.SongInstrumentPart.InstrumentType == ESongInstrumentType.BassGuitar) ? 4 : 6;

            currentStringNotes = new SongNote?[numStrings];
            currentStringFingers = new int[numStrings];
            currentStringDetected = new bool[numStrings];

            stringOffsetSemitones = new double[numStrings];

            double baseOffset = 0;

            if (player.SongTuningMode != ESongTuningMode.None)
            {
                baseOffset = -player.TuningOffsetSemitones;
            }

            for (int str = 0; str < numStrings; str++)
            {
                stringOffsetSemitones[str] = baseOffset + player.SongInstrumentPart.Tuning.StringSemitoneOffsets[str];
            }

            NoteDetector = new NoteDetector((int)ChartPlayerGame.Instance.Plugin.Host.SampleRate);
            NoteDetector.MaxFrequency = (numStrings == 6) ? 2637 : 330;

            noteDetectThread = new Thread(new ThreadStart(NoteDetector.Run));
            noteDetectThread.Name = "NoteDetect";
            noteDetectThread.Start();

            notesDetected = new sbyte[player.SongInstrumentNotes.Notes.Count];

            // Sort notes by time and then by string
            player.SongInstrumentNotes.Notes = player.SongInstrumentNotes.Notes.OrderBy(n => n.TimeOffset).ThenByDescending(n => GetStringOffset(n.String)).ToList();

            // Do a pre-pass on the notes to find spot where we want to add note numbers on the fretboard or re-show the full chord
            SongNote? lastNote = null;
            SongNote?[] lastStringNote = new SongNote?[25];
            float lastTime = currentTime;

            UIColor handPositionColor = UIColor.White;
            handPositionColor.A = 32;

            foreach (SongNote note in player.SongInstrumentNotes.Notes)
            {
                // Avoid not showing chords that are continued
                if (note.Techniques.HasFlag(ESongNoteTechnique.Continued))
                {
                    SongNote? last = (note.String == -1) ? null : lastStringNote[note.String];

                    if (last.HasValue)
                    {
                        if (last.Value.String == note.String)
                        {
                            nonRepeatChords[last.Value.TimeOffset] = true;
                        }
                    }
                }

                if (!lastNote.HasValue)
                {
                    nonRepeatChords[note.TimeOffset] = true;
                    nonRepeatNotes[note.TimeOffset] = true;
                }
                else
                {
                    if (note.HandFret != lastNote.Value.HandFret)
                    {
                        nonRepeatChords[note.TimeOffset] = true;
                        nonRepeatNotes[note.TimeOffset] = true;
                    }
                    else
                    {
                        if (note.Techniques.HasFlag(ESongNoteTechnique.Chord))
                        {
                            // should differentiate base on *some* techniques

                            //if (note.Techniques.HasFlag(ESongNoteTechnique.ChordNote) || (lastNote.Value.ChordID != note.ChordID) || (lastNote.Value.Techniques != note.Techniques) || ((note.TimeOffset - lastNote.Value.TimeOffset) > 1))
                            if ((note.TimeLength > 0) || note.Techniques.HasFlag(ESongNoteTechnique.ChordNote) || (lastNote.Value.ChordID != note.ChordID) || ((note.TimeOffset - lastNote.Value.TimeOffset) > 1))
                            {
                                nonRepeatChords[note.TimeOffset] = true;
                            }
                        }
                        else
                        {
                            if ((note.Fret > 0) && ((lastNote.Value.Fret != note.Fret) || ((note.TimeOffset - lastNote.Value.TimeOffset) > 1)))
                            {
                                nonRepeatNotes[note.TimeOffset] = true;
                            }
                        }

                        if ((note.FingerID != -1) && (lastNote.Value.FingerID != note.FingerID))
                        {
                            nonRepeatChords[note.TimeOffset] = true;
                        }
                    }
                }

                if (note.ChordID != -1)
                {
                    for (int str = 0; str < 6; str++)
                    {
                        lastStringNote[str] = note;
                    }
                }
                else
                {
                    lastStringNote[note.String] = note;
                }

                lastNote = note;
            }
        }

        public override void Stop()
        {
            NoteDetector.Stop();
            noteDetectThread.Join();
        }

        public override void ResetScore(float scoreStartSecs)
        {
            Array.Clear(notesDetected);

            base.ResetScore(scoreStartSecs);
        }

        public override void Draw()
        {
            base.Draw();
        }

        public override void UpdateCamera()
        {
            if (ChartPlayerGame.Instance.Plugin.SongPlayer != null)
            {
                fretCamera.Update(minFret, maxFret, targetFocusFret, -(float)(currentTime * timeScale));
            }
        }

        public override void DrawQuads()
        {
            base.DrawQuads();

            FogEnabled = true;
            FogStart = 400;
            FogEnd = fretCamera.CameraDistance + fretCamera.FocusDist;
            FogColor = UIColor.Black;

            try
            {
                if (player != null)
                {
                    firstNote = null;

                    timeScale = 600 / NoteDisplaySeconds;

                    for (int fret = 0; fret < numFrets; fret++)
                    {
                        DrawFretTimeLine(fret, 0, startTime, endTime, whiteHalfAlpha);
                    }

                    minFret = numFrets;
                    maxFret = 0;

                    DrawBeats();

                    float secsBehind = 1f;

                    float startWithBuffer = startTime - secsBehind;

                    int lastHandFret = -1;
                    float lastTime = currentTime;

                    UIColor handPositionColor = UIColor.White;
                    handPositionColor.A = 32;

                    List<SongNote> allNotes = player.SongInstrumentNotes.Notes;

                    startNotePosition = GetStartNote<SongNote>(currentTime, secsBehind, startNotePosition, allNotes);

                    int pos = 0;

                    // Draw hand position areas on timeline
                    for (pos = startNotePosition; pos < allNotes.Count; pos++)
                    {
                        SongNote note = allNotes[pos];

                        if (note.TimeOffset > endTime)
                            break;

                        if ((note.TimeOffset > currentTime) && ((lastHandFret != -1) && (lastHandFret != note.HandFret)))
                        {
                            if (DisplayNotes)
                            {
                                DrawFlatImage(Layout.Current.GetImage("SingleWhitePixel"), lastHandFret - 1, lastHandFret + 3, lastTime, note.TimeOffset, 0, handPositionColor);
                            }
                            lastTime = note.TimeOffset;
                        }

                        lastHandFret = note.HandFret;
                    }

                    if (lastHandFret != -1)
                    {
                        if (DisplayNotes)
                        {
                            DrawFlatImage(Layout.Current.GetImage("SingleWhitePixel"), lastHandFret - 1, lastHandFret + 3, lastTime, endTime, 0, handPositionColor);
                        }
                    }
                
                    // Clear our string info
                    for (int str = 0; str < numStrings; str++)
                    {
                        currentStringNotes[str] = null;
                        currentStringFingers[str] = -1;
                        currentStringDetected[str] = false;
                    }

                    int lastNote = pos;

                    if (lastNote == allNotes.Count)
                        lastNote--;

                    currentChordNote = null;
                    currentFingerNote = null;
                    currentChordDetected = false;

                    for (pos = lastNote; pos >= startNotePosition; pos--)
                    {
                        SongNote note = allNotes[pos];

                        if (note.TimeOffset > currentTime)
                            continue;

                        if (note.Techniques.HasFlag(ESongNoteTechnique.Chord))
                        {
                            if ((note.TimeOffset <= currentTime) && !currentChordNote.HasValue)
                                currentChordNote = note;
                        }
                        else
                        {
                            if (!currentStringNotes[note.String].HasValue)
                            {
                                if ((note.TimeOffset + Math.Max(note.TimeLength, 0.2f) > currentTime))
                                    currentStringNotes[note.String] = note;
                            }
                        }

                        if (note.TimeOffset <= currentTime)
                        {
                            if (note.FingerID != -1)
                            {
                                if (!currentFingerNote.HasValue)
                                    currentFingerNote = note;
                            }
                        }
                    }

                    if (currentChordNote.HasValue)
                    {
                        currentChordDetected = NoteDetect(currentChordNote.Value);
                    }

                    for (int str = 0; str < numStrings; str++)
                    {
                        if (currentStringNotes[str].HasValue)
                        {
                            currentStringDetected[str] = NoteDetect(currentStringNotes[str].Value);
                        }
                    }

                    // Draw the notes
                    if (startNotePosition < allNotes.Count)
                    {
                        for (pos = lastNote; pos >= startNotePosition; pos--)
                        {
                            SongNote note = allNotes[pos];

                            if (note.TimeOffset > endTime)
                                continue;

                            isDetected = false;

                            if (note.TimeOffset <= currentTime)
                            {
                                isDetected = note.Techniques.HasFlag(ESongNoteTechnique.Chord) ? currentChordDetected : (currentStringDetected[note.String] && (currentStringNotes[note.String].Value.EndTime == note.EndTime));
                            }

                            if (DisplayNotes)
                            {
                                DrawNote(note);
                            }

                            // Check to see if we should mark as detected
                            if (isDetected && (notesDetected[pos] != 1))
                            {
                                if (notesDetected[pos] == 0)
                                    NumNotesTotal++;

                                NumNotesDetected++;

                                notesDetected[pos] = 1;
                            }
                        }

                        // Check for missed notes
                        for (; pos >= 0; pos--)
                        {
                            if (notesDetected[pos] != 0)
                                break;

                            if (allNotes[pos].TimeOffset < scoreStartSecs)
                                break;

                            notesDetected[pos] = -1;

                            NumNotesTotal++;
                        }
                    }

                    // Draw the front section
                    for (int str = 0; str < numStrings; str++)
                    {
                        UIColor color = stringColors[str];
                        color.A = 192;

                        DrawFretHorizontalLine(0, numFrets, startTime, GetStringHeight(GetStringOffset(str)), color, .04f);
                    }

                    for (int fret = 1; fret < numFrets; fret++)
                    {
                        DrawFretVerticalLine(fret - 1, startTime, GetStringHeight(0), GetStringHeight(numStrings - 1), whiteHalfAlpha);

                        UIColor color = UIColor.White;

                        if (firstNote.HasValue && (fret >= firstNote.Value.HandFret) && (fret < (firstNote.Value.HandFret + 4)))
                        {
                        }
                        else
                        {
                            color.A = 64;
                        }

                        DrawVerticalText(numberStrings[fret], fret - 0.5f, 0, currentTime, color, 0.08f);
                    }

                    if (DisplayNotes)
                    {
                        // Draw current notes
                        if (currentChordNote.HasValue)
                        {
                            isDetected = currentChordDetected;

                            DrawChordOutline(currentChordNote.Value);

                            if (!currentChordNote.Value.Techniques.HasFlag(ESongNoteTechnique.ChordNote))
                                DrawChordNotes(currentChordNote.Value, player.SongInstrumentNotes.Chords[currentChordNote.Value.ChordID], drawCurrent: true, isGhost: false);
                        }

                        for (int str = 0; str < numStrings; str++)
                        {
                            if (currentStringNotes[str].HasValue)
                            {
                                SongNote stringNote = currentStringNotes[str].Value;

                                isDetected = currentStringDetected[str];

                                DrawSingleNote(stringNote, drawCurrent: true, isGhost: false);
                            }
                        }

                        // Draw fingering
                        int fingerID = -1;
                        SongNote fingerNote = new SongNote();

                        if (currentChordNote.HasValue)
                        {
                            fingerID = currentChordNote.Value.ChordID;
                            fingerNote = currentChordNote.Value;
                        }
                        else if (currentFingerNote.HasValue)
                        {
                            fingerID = currentFingerNote.Value.FingerID;
                            fingerNote = currentFingerNote.Value;
                        }

                        if (fingerID != -1)
                        {
                            SongChord fingerChord = player.SongInstrumentNotes.Chords[fingerID];

                            for (int str = 0; str < fingerChord.Fingers.Count; str++)
                            {
                                if ((fingerChord.Fingers[str] != -1) && (fingerChord.Frets[str] != -1))
                                {
                                    SongNote stringNote = (currentStringNotes[str].HasValue && (currentStringNotes[str].Value.Fret == fingerChord.Frets[str]) && (currentStringNotes[str].Value.TimeOffset >= fingerNote.TimeOffset)) ? currentStringNotes[str].Value :
                                        new SongNote()
                                        {
                                            ChordID = fingerNote.ChordID,
                                            TimeOffset = fingerNote.TimeOffset,
                                            TimeLength = fingerNote.TimeLength,
                                            Fret = fingerChord.Frets[str],
                                            String = str,
                                            Techniques = fingerNote.Techniques,
                                            HandFret = fingerNote.HandFret
                                        };

                                    float drawFret = stringNote.Fret;

                                    if (stringNote.SlideFret != -1)
                                    {
                                        drawFret = GetSlideFret(stringNote);
                                    }

                                    DrawVerticalImage(Layout.Current.GetImage("FingerOutline"), drawFret - 0.5f, currentTime, GetNoteHeadHeight(stringNote), UIColor.White, 0.05f);
                                    DrawVerticalText(fingerChord.Fingers[str].ToString(), drawFret - 0.5f, GetNoteHeadHeight(stringNote), currentTime, UIColor.White, 0.05f);
                                }
                            }

                            DrawChordOutline(fingerNote);
                        }

                        if (firstNote.HasValue)
                        {
                            targetFocusFret = (float)firstNote.Value.HandFret + 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Layout.Current.ShowContinuePopup("Draw error: \n\n" + ex.ToString());
            }
        }

        void DrawNote(SongNote note)
        {
            if (note.Techniques.HasFlag(ESongNoteTechnique.Chord))
            {
                //if ((note.TimeOffset <= currentTime) && !currentChordNote.HasValue)
                //    currentChordNote = note;

                if (!note.Techniques.HasFlag(ESongNoteTechnique.ChordNote))
                {
                    DrawChordNotes(note, player.SongInstrumentNotes.Chords[note.ChordID]);
                }

                if (note.TimeOffset > currentTime)
                {
                    DrawChordOutline(note);
                }
            }
            else
            {
                if ((note.TimeOffset > currentTime) && (note.Fret > 0) && nonRepeatNotes.ContainsKey(note.TimeOffset))
                    DrawVerticalText(numberStrings[note.Fret], note.Fret - 0.5f, 0, note.TimeOffset, UIColor.White, 0.12f);

                DrawSingleNote(note);
            }

            if (note.FingerID != -1)
            {
                if (note.TimeOffset <= currentTime)
                {
                    //if (!currentFingerNote.HasValue)
                    //    currentFingerNote = note;
                }
                else
                {
                    if (nonRepeatChords.ContainsKey(note.TimeOffset))
                    {
                        SongChord fingerChord = player.SongInstrumentNotes.Chords[note.FingerID];

                        DrawChordNotes(note, fingerChord, drawCurrent: false, isGhost: true);

                        DrawChordOutline(note);
                    }
                }
            }
        }

        void DrawChordNotes(SongNote note, SongChord chord)
        {
            DrawChordNotes(note, chord, drawCurrent: false, isGhost: false);
        }

        void DrawChordNotes(SongNote note, SongChord chord, bool drawCurrent, bool isGhost)
        {
            for (int str = 0; str < chord.Fingers.Count; str++)
            {
                if ((chord.Fingers[str] != -1) || (chord.Frets[str] != -1) && (chord.Frets[str] < numFrets))
                {
                    SongNote chordNote = new SongNote()
                    {
                        ChordID = note.ChordID,
                        TimeOffset = note.TimeOffset,
                        TimeLength = note.TimeLength,
                        Fret = chord.Frets[str],
                        String = str,
                        Techniques = note.Techniques,
                        HandFret = note.HandFret
                    };

                    if (nonRepeatChords.ContainsKey(note.TimeOffset) || (note.TimeOffset <= currentTime))
                    {
                        if ((note.TimeOffset > currentTime) && (chord.Frets[str] > 0))
                            DrawVerticalText(numberStrings[chord.Frets[str]], chord.Frets[str] - 0.5f, 0, note.TimeOffset, UIColor.White, 0.12f);

                        DrawSingleNote(chordNote, drawCurrent, isGhost);
                    }
                }
            }
        }

        void DrawChordOutline(SongNote note)
        {
            int chordID = note.ChordID;

            if (chordID == -1)
                chordID = note.FingerID;

            SongChord chord = player.SongInstrumentNotes.Chords[chordID];

            float timeOffset = Math.Max(note.TimeOffset, currentTime);

            UIColor color = UIColor.White;
            color.A = 64;

            if (note.Techniques.HasFlag(ESongNoteTechnique.Accent))
            {
                color.A = 255;
            }

            if (nonRepeatChords.ContainsKey(note.TimeOffset) || (timeOffset == currentTime))
            {
                DrawVerticalNinePatch(Layout.Current.GetImage("ChordOutline"), note.HandFret - 1, note.HandFret + 3, timeOffset, 0, GetStringHeight(numStrings), isDetected ? UIColor.White : color);

                if (!String.IsNullOrEmpty(chord.Name))
                    DrawVerticalText(chord.Name, note.HandFret - 1.02f, GetStringHeight(numStrings - 1), timeOffset, UIColor.White, 0.09f, rightAlign: true);
            }
            else
            {
                DrawVerticalNinePatch(Layout.Current.GetImage("ChordOutline"), note.HandFret - 1, note.HandFret + 3, timeOffset, 0, GetStringHeight(2), isDetected ? UIColor.White : color);

                if (note.Techniques.HasFlag(ESongNoteTechnique.PalmMute) || note.Techniques.HasFlag(ESongNoteTechnique.FretHandMute))
                {
                    DrawVerticalImage(Layout.Current.GetImage(note.Techniques.HasFlag(ESongNoteTechnique.PalmMute) ? "NotePalmMute" : "NoteMute"), note.HandFret + 1, timeOffset, GetStringHeight(.5f), whiteHalfAlpha, 0.15f);
                }
            }
        }

        void DrawSingleNote(SongNote note)
        {
            DrawSingleNote(note, drawCurrent: false, isGhost: false);
        }

        void DrawSingleNote(SongNote note, bool drawCurrent, bool isGhost)
        {
            bool isCurrent = (note.TimeOffset <= currentTime);

            //if ((isCurrent && !drawCurrent) && (!note.Techniques.HasFlag(ESongNoteTechnique.Chord) || note.Techniques.HasFlag(ESongNoteTechnique.ChordNote)))
            //{
            //    if (!currentStringNotes[note.String].HasValue)
            //        currentStringNotes[note.String] = note;
            //}

            UIColor stringColor = UIColor.White;

            if (!isDetected)
            {
                float dimAmount = 0.25f;

                float timeDiff = currentTime - note.TimeOffset;

                if ((timeDiff > 0) && (timeDiff < 0.1f))
                {
                    dimAmount -= (0.1f - timeDiff) * 2;
                }

                stringColor = UIColor.Lerp(UIColor.White, stringColors[note.String], dimAmount);
                stringColor = UIColor.Lerp(stringColor, UIColor.Black, dimAmount);
            }

            if (isGhost)
            {
                stringColor.A = 32;
            }

            float noteHeadTime = Math.Max(note.TimeOffset, currentTime);
            float noteSustain = note.TimeLength - (noteHeadTime - note.TimeOffset);

            if (noteSustain < 0)
                noteSustain = 0;

            UIImage modifierImage = null;

            bool isMuted = false;
            bool isSlide = false;
            int slideTo = 0;

            if (note.Techniques.HasFlag(ESongNoteTechnique.HammerOn))
            {
                modifierImage = Layout.Current.GetImage("NoteHammerOn");
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.PullOff))
            {
                modifierImage = Layout.Current.GetImage("NotePullOff");
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.FretHandMute))
            {
                modifierImage = Layout.Current.GetImage("NoteMute");

                isMuted = true;
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.PalmMute))
            {
                modifierImage = Layout.Current.GetImage("NotePalmMute");

                isMuted = true;
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.Harmonic))
            {
                modifierImage = Layout.Current.GetImage("NoteHarmonic");
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.PinchHarmonic))
            {
                modifierImage = Layout.Current.GetImage("NotePinchHarmonic");
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.Slide))
            {
                isSlide = true;
                slideTo = note.SlideFret;
            }

            if (isMuted && !isDetected)
            {
                stringColor = UIColor.Lerp(stringColor, UIColor.Black, 0.5f);
            }

            if (note.Techniques.HasFlag(ESongNoteTechnique.Accent))
            {
                stringColor = UIColor.Lerp(stringColor, UIColor.White, 0.75f);
            }

            int stringOffset = GetStringOffset(note.String);

            float drawFret = note.Fret;

            if (note.Fret == 0)   // Open string
            {
                drawFret = (float)note.HandFret + 1 + 0.5f;

                if (!(drawCurrent && isCurrent) && (note.TimeOffset + note.TimeLength > currentTime))
                {
                    minFret = Math.Min(minFret, note.HandFret);
                    maxFret = Math.Max(maxFret, note.HandFret + 3);

                    // Sustain note tail
                    DrawFlatImage(stringNoteTrailImages[note.String], drawFret - 0.5f, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(stringOffset), stringColor, .05f);
                }

                if (!isCurrent || drawCurrent)
                {
                    // Note head
                    if (isDetected)
                        DrawVerticalImage(Layout.Current.GetImage("GuitarDetected"), note.HandFret - 1, note.HandFret + 3, noteHeadTime, GetStringHeight(stringOffset), stringColor, 0.06f);

                    DrawVerticalImage(stringNoteImages[note.String], note.HandFret - 1, note.HandFret + 3, noteHeadTime, GetStringHeight(stringOffset), stringColor, 0.04f);
                }
            }
            else    // Fretted note
            {
                if (isSlide && (note.TimeOffset < currentTime))
                {
                    drawFret = GetSlideFret(note);
                }

                if (!(drawCurrent && isCurrent) && (note.TimeOffset + note.TimeLength > currentTime))
                {
                    minFret = Math.Min(minFret, drawFret);
                    maxFret = Math.Max(maxFret, drawFret);

                    // Sustain note tail
                    if (isSlide)
                    {
                        //DrawSkewedFlatImage(PixGame.Instance.GetImage(trailImageName), note.Fret - 0.5f, slideTo - 0.5f, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(note.String), stringColor);

                        DrawImageTrail(stringNoteTrailImages[note.String], stringColor, 0.03f,
                            new Vector3(drawFret - 0.5f, GetStringHeight(stringOffset), noteHeadTime), new Vector3(slideTo - 0.5f, GetStringHeight(stringOffset), noteHeadTime + noteSustain));
                    }
                    else
                    {
                        if ((note.CentsOffsets != null) && (note.CentsOffsets.Length > 0))
                        {
                            DrawBend(stringNoteTrailImages[note.String], note.Fret - 0.5f, note.TimeOffset, note.TimeLength, stringOffset, note.CentsOffsets, stringColor);
                        }
                        else
                        {
                            if (note.Techniques.HasFlag(ESongNoteTechnique.Vibrato))
                            {
                                DrawVibrato(stringNoteTrailImages[note.String], note.Fret - 0.5f, note.TimeOffset, note.TimeOffset + note.TimeLength, GetStringHeight(stringOffset), stringColor);
                            }
                            else
                            {
                                DrawFlatImage(stringNoteTrailImages[note.String], note.Fret - 0.5f, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(stringOffset), stringColor, .03f);
                            }
                        }
                    }
                }

                if (!isCurrent || drawCurrent)
                {
                    float noteHeadHeight = GetNoteHeadHeight(note);

                    // Note head
                    if (!note.Techniques.HasFlag(ESongNoteTechnique.Continued) || (note.TimeOffset < currentTime))
                    //if (nonReapeatDict.ContainsKey(note.TimeOffset) || (note.TimeOffset < currentTime))
                    {
                        if (isDetected)
                            DrawVerticalImage(Layout.Current.GetImage("GuitarDetected"), drawFret - 0.5f, noteHeadTime, noteHeadHeight, stringColor, 0.1f);

                        DrawVerticalImage(stringNoteImages[note.String], drawFret - 0.5f, noteHeadTime, noteHeadHeight, stringColor, 0.08f);
                    }
                }
            }

            if (!isCurrent || drawCurrent)
            {
                // Note Modifier
                if (modifierImage != null)
                    DrawVerticalImage(modifierImage, drawFret - 0.5f, noteHeadTime, GetStringHeight(stringOffset), UIColor.White, 0.08f);
            }

            if (!isCurrent)
            {
                // Note "shadow" on fretboard
                DrawFretHorizontalLine(drawFret - 1, drawFret, noteHeadTime, 0, whiteHalfAlpha, 0.08f);

                // Small lines for lower strings
                for (int prevString = 0; prevString < stringOffset; prevString++)
                {
                    DrawFretHorizontalLine(drawFret - 0.6f, drawFret - 0.4f, noteHeadTime, GetStringHeight(prevString), whiteHalfAlpha, .04f);
                }
            }

            // Vertical line from fretboard up to note head
            DrawFretVerticalLine(drawFret - 0.5f, noteHeadTime, 0, GetStringHeight(stringOffset), whiteHalfAlpha);

            if (note.TimeOffset > currentTime)
                firstNote = note;
        }

        static float scaleLength = 300.0f;

        public static float GetFretPosition(float fret)
        {
            return scaleLength - (scaleLength / (float)(Math.Pow(2, (double)fret / 12.0)));
        }

        int GetStringOffset(int str)
        {
            if (ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.InvertStrings)
                return numStrings - str - 1;

            return str;
        }

        float GetStringHeight(float str)
        {
            return 3.0f + (str * 4.0f);
        }

        void DrawFretHorizontalLine(float startFret, float endFret, float time, float heightOffset, UIColor color, float imageScale)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            time *= -timeScale;

            UIImage image = Layout.Current.GetImage("HorizontalFretLine");

            float minZ = time + ((float)image.Height * imageScale);
            float maxZ = time - ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startFret, heightOffset, minZ), color, new Vector3(startFret, heightOffset, maxZ), color, new Vector3(endFret, heightOffset, maxZ), color, new Vector3(endFret, heightOffset, minZ), color);
        }

        void DrawFretVerticalLine(float fretCenter, float time, float startHeight, float endEndHeight, in UIColor color)
        {
            fretCenter = GetFretPosition(fretCenter);
            time *= -timeScale;

            UIImage image = Layout.Current.GetImage("VerticalFretLine");

            float imageScale = .03f;

            float minX = (float)fretCenter - ((float)image.Width * imageScale);
            float maxX = (float)fretCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, startHeight, time), color, new Vector3(minX, endEndHeight, time), color, new Vector3(maxX, endEndHeight, time), color, new Vector3(maxX, startHeight, time), color);
        }

        void DrawFretTimeLine(float fretCenter, float height, float startTime, float endTime, in UIColor color)
        {
            fretCenter = GetFretPosition(fretCenter);
            startTime *= -timeScale;
            endTime *= -timeScale;

            UIImage image = Layout.Current.GetImage("VerticalFretLine");

            float imageScale = 0.03f;

            float minX = (float)fretCenter - ((float)image.Width * imageScale);
            float maxX = (float)fretCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, height, startTime), color, new Vector3(minX, height, endTime), color, new Vector3(maxX, height, endTime), color, new Vector3(maxX, height, startTime), color);
        }

        void DrawVerticalImage(UIImage image, float startFret, float endFret, float time, float heightOffset, in UIColor color, float imageScale)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            time *= -timeScale;

            float minY = heightOffset - ((float)image.Height * imageScale);
            float maxY = heightOffset + ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startFret, minY, time), color, new Vector3(startFret, maxY, time), color, new Vector3(endFret, maxY, time), color, new Vector3(endFret, minY, time), color);
        }

        void DrawVerticalImage(UIImage image, float fretCenter, float timeCenter, float heightOffset, in UIColor color, float imageScale)
        {
            fretCenter = GetFretPosition(fretCenter);
            timeCenter *= -timeScale;

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            float minY = heightOffset - ((float)image.Height * imageScale);
            float maxY = heightOffset + ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(minX, minY, timeCenter), color, new Vector3(minX, maxY, timeCenter), color, new Vector3(maxX, maxY, timeCenter), color, new Vector3(maxX, minY, timeCenter), color);
        }

        void DrawVerticalImage(UIImage image, float startFret, float endFret, float time, float startHeight, float endHeight, in UIColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            time *= -timeScale;

            DrawQuad(image, new Vector3(startFret, startHeight, time), color, new Vector3(startFret, endHeight, time), color, new Vector3(endFret, endHeight, time), color, new Vector3(endFret, startHeight, time), color);
        }

        void DrawVerticalNinePatch(UIImage image, float startFret, float endFret, float time, float startHeight, float endHeight, in UIColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            time *= -timeScale;

            DrawNinePatch(image, image.Width / 2, image.Height / 2, new Vector3(startFret, startHeight, time), new Vector3(startFret, endHeight, time), new Vector3(endFret, endHeight, time), new Vector3(endFret, startHeight, time), color);
        }

        void DrawFlatImage(UIImage image, float fretCenter, float startTime, float endTime, float heightOffset, in UIColor color, float imageScale)
        {
            fretCenter = GetFretPosition(fretCenter);
            startTime *= -timeScale;
            endTime *= -timeScale;

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, heightOffset, startTime), color, new Vector3(minX, heightOffset, endTime), color, new Vector3(maxX, heightOffset, endTime), color, new Vector3(maxX, heightOffset, startTime), color);
        }

        void DrawFlatImage(UIImage image, float startFret, float endFret, float startTime, float endTime, float heightOffset, in UIColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            startTime *= -timeScale;
            endTime *= -timeScale;

            DrawQuad(image, new Vector3(startFret, heightOffset, startTime), color, new Vector3(startFret, heightOffset, endTime), color, new Vector3(endFret, heightOffset, endTime), color, new Vector3(endFret, heightOffset, startTime), color);
        }

        void DrawSkewedFlatImage(UIImage image, float startFret, float endFret, float startTime, float endTime, float heightOffset, in UIColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            startTime *= -timeScale;
            endTime *= -timeScale;

            float imageScale = .03f;

            DrawQuad(image, new Vector3(startFret - ((float)image.Width * imageScale), heightOffset, startTime), color, new Vector3(endFret - ((float)image.Width * imageScale), heightOffset, endTime), color,
                new Vector3(endFret + ((float)image.Width * imageScale), heightOffset, endTime), color, new Vector3(startFret + ((float)image.Width * imageScale), heightOffset, startTime), color);
        }

        void DrawImageTrail(UIImage image, UIColor color, float imageScale, params Vector3[] trailPoints)
        {
            Vector3? lastPoint = null;

            foreach (Vector3 point in trailPoints)
            {
                if (lastPoint.HasValue)
                {
                    DrawQuad(image, new Vector3(GetFretPosition(lastPoint.Value.X) - ((float)image.Width * imageScale), lastPoint.Value.Y, lastPoint.Value.Z * -timeScale), color,
                        new Vector3(GetFretPosition(point.X) - ((float)image.Width * imageScale), point.Y, point.Z * -timeScale), color,
                        new Vector3(GetFretPosition(point.X) + ((float)image.Width * imageScale), point.Y, point.Z * -timeScale), color,
                        new Vector3(GetFretPosition(lastPoint.Value.X) + ((float)image.Width * imageScale), lastPoint.Value.Y, lastPoint.Value.Z * -timeScale), color);
                }

                lastPoint = point;
            }
        }

        void DrawVibrato(UIImage image, float fretCenter, float startTime, float endTime, float heightOffset, in UIColor color)
        {
            float imageScale = .03f;

            fretCenter = GetFretPosition(fretCenter);

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            float lastTime = startTime;
            float lastHeight = heightOffset;

            int numPoints = (int)((endTime - startTime) / 0.02f);

            for (int i = 1; i <= numPoints; i++)
            {
                float time = MathUtil.Lerp(startTime, endTime, (float)i / (float)numPoints);

                if (time < currentTime)
                {
                    lastTime = currentTime;

                    continue;
                }

                float height = heightOffset + ((float)Math.Sin((time - startTime) * 50) * 1);

                DrawQuad(image, new Vector3(minX, lastHeight, lastTime * -timeScale), color,
                    new Vector3(minX, height, time * -timeScale), color,
                    new Vector3(maxX, height, time * -timeScale), color,
                    new Vector3(maxX, lastHeight, lastTime * -timeScale), color);

                lastHeight = height;
                lastTime = time;
            }
        }

        float GetSlideFret(in SongNote note)
        {
            return MathUtil.Lerp(note.Fret, note.SlideFret, MathUtil.Saturate((currentTime - note.TimeOffset) / note.TimeLength));
        }

        float GetCentsOffset(float strng, float cents)
        {
            if (strng < 2)
                return cents / 30.0f;

            return cents / -30.0f;
        }

        float GetBendCents(float startTime, float strng, in CentsOffset[] bendOffsets)
        {
            if (startTime > currentTime)
            {
                return (bendOffsets[0].TimeOffset == startTime) ? bendOffsets[0].Cents : 0;
            }

            int lastCents = 0;
            float lastTime = startTime;

            foreach (CentsOffset offset in bendOffsets)
            {
                if (offset.TimeOffset >= currentTime)
                {
                    float timePercent = (offset.TimeOffset - currentTime) / (offset.TimeOffset - lastTime);

                    return MathUtil.Lerp((float)offset.Cents, (float)lastCents, timePercent);
                }

                lastCents = offset.Cents;
                lastTime = offset.TimeOffset;
            }

            return lastCents;
        }

        float GetNoteHeadHeight(in SongNote note)
        {
            float noteHeadHeight = GetStringHeight(GetStringOffset(note.String));

            if ((note.CentsOffsets != null) && (note.CentsOffsets.Length > 0))
            {
                float bendOffset = GetCentsOffset(note.String, GetBendCents(note.TimeOffset, note.String, note.CentsOffsets));

                if (ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.InvertStrings)
                    noteHeadHeight -= bendOffset;
                else
                    noteHeadHeight += bendOffset;
            }

            return noteHeadHeight;
        }

        void DrawBend(UIImage image, float fretCenter, float startTime, float sustain, float strng, in CentsOffset[] bendOffsets, in UIColor color)
        {
            float endTime = startTime + sustain;

            if (endTime < currentTime)
                return;

            fretCenter = GetFretPosition(fretCenter);

            float heightOffset = GetStringHeight(strng);

            float imageScale = .03f;

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            float lastTime = startTime;
            float lastHeight = heightOffset;

            foreach (CentsOffset offset in bendOffsets)
            {
                float height = heightOffset + GetCentsOffset(strng, offset.Cents);

                if ((offset.TimeOffset >= currentTime) && (offset.TimeOffset > lastTime))
                {
                    if (lastTime < currentTime)
                    {
                        float timePercent = (currentTime - lastTime) / (offset.TimeOffset - lastTime);

                        lastHeight = MathUtil.Lerp(lastHeight, height, timePercent);

                        lastTime = currentTime;
                    }

                    DrawQuad(image, new Vector3(minX, lastHeight, lastTime * -timeScale), color,
                        new Vector3(minX, height, offset.TimeOffset * -timeScale), color,
                        new Vector3(maxX, height, offset.TimeOffset * -timeScale), color,
                        new Vector3(maxX, lastHeight, lastTime * -timeScale), color);
                }

                lastHeight = height;
                lastTime = offset.TimeOffset;
            }

            if (lastTime < endTime)
            {
                lastTime = Math.Max(lastTime, currentTime);

                DrawQuad(image, new Vector3(minX, lastHeight, lastTime * -timeScale), color,
                    new Vector3(minX, lastHeight, endTime * -timeScale), color,
                    new Vector3(maxX, lastHeight, endTime * -timeScale), color,
                    new Vector3(maxX, lastHeight, lastTime * -timeScale), color);
            }
        }

        void DrawFlatText(string text, float fretCenter, float timeCenter, float heightOffset, in UIColor color, float imageScale)
        {
            fretCenter = GetFretPosition(fretCenter);
            timeCenter *= -timeScale;

            UIFont font = Layout.Current.GetFont("LargeFont");

            float textWidth;
            float textHeight;

            font.SpriteFont.MeasureString(text, out textWidth, out textHeight);

            textWidth *= imageScale;
            textHeight *= imageScale;

            float x = fretCenter - (textWidth / 2);

            Rectangle drawRect = Rectangle.Empty;

            int start = 0;
            int end = text.Length;
            int inc = 1;

            if (LeftyMode)
            {
                start = text.Length - 1;
                end = -1;
                inc = -1;
            }

            for (int i = start; i != end; i += inc)
            {
                float z = timeCenter - (textHeight / 2);

                char c = text[i];

                SpriteFontGlyph glyph = font.SpriteFont.GetGlyph(c);

                drawRect.X = glyph.X;
                drawRect.Y = glyph.Y;
                drawRect.Width = glyph.Width;
                drawRect.Height = glyph.Height;

                if (LeftyMode)
                {
                    DrawQuad(font.SpriteFont.FontImage, drawRect, new Vector3(x + ((float)glyph.Width * imageScale), heightOffset, z), color, new Vector3(x + ((float)glyph.Width * imageScale), heightOffset, z + ((float)glyph.Height * imageScale)), color,
                        new Vector3(x, heightOffset, z + ((float)glyph.Height * imageScale)), color,
                        new Vector3(x, heightOffset, z), color);
                }
                else
                {
                    DrawQuad(font.SpriteFont.FontImage, drawRect, new Vector3(x, heightOffset, z), color, new Vector3(x, heightOffset, z + ((float)glyph.Height * imageScale)), color,
                        new Vector3(x + ((float)glyph.Width * imageScale), heightOffset, z + ((float)glyph.Height * imageScale)), color,
                        new Vector3(x + ((float)glyph.Width * imageScale), heightOffset, z), color);

                }

                x += (glyph.Width + font.SpriteFont.Spacing) * imageScale;
            }
        }

        void DrawVerticalText(string text, float fretCenter, float verticalCenter, float timeCenter, in UIColor color, float imageScale)
        {
            DrawVerticalText(text, fretCenter, verticalCenter, timeCenter, color, imageScale, rightAlign: false);
        }

        void DrawVerticalText(string text, float fretCenter, float verticalCenter, float timeCenter, in UIColor color, float imageScale, bool rightAlign)
        {
            fretCenter = GetFretPosition(fretCenter);
            timeCenter *= -timeScale;

            UIFont font = Layout.Current.GetFont("LargeFont");

            float textWidth;
            float textHeight;

            font.SpriteFont.MeasureString(text, imageScale, out textWidth, out textHeight);

            float x = 0;
            
            if (rightAlign)
            {
                x = fretCenter - textWidth;
            }
            else
            {
                x = fretCenter - (textWidth / 2);
            }

            Rectangle drawRect = Rectangle.Empty;

            int start = 0;
            int end = text.Length;
            int inc = 1;

            if (LeftyMode)
            {
                start = text.Length - 1;
                end = -1;
                inc = -1;
            }

            for (int i = start; i != end; i += inc)
            {
                float y = verticalCenter - (textHeight / 2);

                char c = text[i];

                SpriteFontGlyph glyph = font.SpriteFont.GetGlyph(c);

                drawRect.X = glyph.X;
                drawRect.Y = glyph.Y;
                drawRect.Width = glyph.Width;
                drawRect.Height = glyph.Height;

                if (LeftyMode)
                {
                    DrawQuad(font.SpriteFont.FontImage, drawRect, new Vector3(x + ((float)glyph.Width * imageScale), y, timeCenter), color, new Vector3(x + ((float)glyph.Width * imageScale), y + ((float)glyph.Height * imageScale), timeCenter), color,
                        new Vector3(x, y + ((float)glyph.Height * imageScale), timeCenter), color, new Vector3(x, y, timeCenter), color);
                }
                else
                {
                    DrawQuad(font.SpriteFont.FontImage, drawRect, new Vector3(x, y, timeCenter), color, new Vector3(x, y + ((float)glyph.Height * imageScale), timeCenter), color,
                        new Vector3(x + ((float)glyph.Width * imageScale), y + ((float)glyph.Height * imageScale), timeCenter), color, new Vector3(x + ((float)glyph.Width * imageScale), y, timeCenter), color);
                }
                

                x += (glyph.Width + font.SpriteFont.Spacing) * imageScale;
            }
        }

        static int[] GuitarStringNotes = { 40, 45, 50, 55, 59, 64 };
        static int[] BassStringNotes = { 28, 33, 38, 43 };

        double GetNoteFrequency(int strng, double fret, double semitoneOffset)
        {
            semitoneOffset = fret + stringOffsetSemitones[strng] + semitoneOffset;

            if (numStrings == 6)
            {
                return NoteUtil.GetMidiNoteFrequency(GuitarStringNotes[strng] + semitoneOffset);
            }
            else
            {
                return NoteUtil.GetMidiNoteFrequency(BassStringNotes[strng] + semitoneOffset);
            }
        }

        double[] freqs = new double[6];

        bool NoteDetect(in SongNote note)
        {                
            if (note.ChordID != -1)
            {
                SongChord chord = player.SongInstrumentNotes.Chords[note.ChordID];

                int numNotes = 0;

                for (int str = 0; str < chord.Fingers.Count; str++)
                {
                    if ((chord.Fingers[str] != -1) || (chord.Frets[str] != -1))
                    {
                        numNotes++;
                    }
                }

                int pos = 0;

                for (int str = 0; str < chord.Fingers.Count; str++)
                {
                    if ((chord.Fingers[str] != -1) || (chord.Frets[str] != -1))
                    {
                        double freq = GetNoteFrequency(str, chord.Frets[str], DetectSemitoneOffset);

                        freqs[pos++] = freq;
                    }
                }

                bool detected = NoteDetector.NoteDetect(freqs, pos);

                return detected;
            }

            if (note.Techniques.HasFlag(ESongNoteTechnique.FretHandMute) || note.Techniques.HasFlag(ESongNoteTechnique.PalmMute))
            {
                return NoteDetector.NoteDetect(0);
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.Slide))
            {
                return NoteDetector.NoteDetect(GetNoteFrequency(note.String, GetSlideFret(note), DetectSemitoneOffset));
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.Bend))
            {
                float centsOffset = GetBendCents(note.TimeOffset, note.SlideFret, note.CentsOffsets);

                double freq = GetNoteFrequency(note.String, note.Fret, DetectSemitoneOffset + (centsOffset / 100));

                return NoteDetector.NoteDetect(freq);
            }

            return NoteDetector.NoteDetect(GetNoteFrequency(note.String, note.Fret, DetectSemitoneOffset));
        }
    }
}
