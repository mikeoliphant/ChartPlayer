using System;
using System.Collections.Generic;
using System.Linq;
using SongFormat;

namespace ChartPlayer
{
    /// <summary>
    /// Musical function categories for notes - determines removal priority
    /// </summary>
    public enum NoteFunction
    {
        // Highest Priority (removed last)
        RootNote = 100,
        SignatureRiff = 95,
        MelodyPrimary = 90,
        DownbeatAnchor = 85,

        // High Priority
        ChordThird = 75,
        ChordFifth = 70,
        MelodySecondary = 65,
        BassLine = 60,

        // Medium Priority
        RhythmicFill = 50,
        ChordExtension = 45,
        PassingTone = 40,

        // Lower Priority (removed early)
        ChordVoicing = 30,
        ApproachNote = 25,
        GraceNote = 20,

        // Lowest Priority (removed first)
        Ornament = 15,
        DoubleStopSecondary = 10,
        UnisonDuplicate = 5,
        GhostNote = 3
    }

    /// <summary>
    /// Difficulty tier with associated rules
    /// </summary>
    public class DifficultyTier
    {
        public string Name { get; set; }
        public int MinPercent { get; set; }
        public int MaxPercent { get; set; }
        public int MaxNotesPerBeat { get; set; }
        public float MinNoteDuration { get; set; }
        public int MaxFretSpan { get; set; }
        public int MaxSimultaneousNotes { get; set; }
        public HashSet<ESongNoteTechnique> AllowedTechniques { get; set; }
        public bool AllowPositionShifts { get; set; }
        public bool AllowStringSkipping { get; set; }
    }

    /// <summary>
    /// Advanced note filtering algorithm based on musical analysis
    /// </summary>
    public class NoteFilter
    {
        public float DifficultyPercent { get; set; } = 100.0f;

        private SongInstrumentNotes originalNotes;
        private List<SongBeat> beats;
        private List<SongSection> sections;
        private Dictionary<int, float> noteImportanceScores;
        private List<(float time, bool isMeasure)> beatGrid;

        // Scoring weights from spec
        private const float WEIGHT_FUNCTION = 0.35f;
        private const float WEIGHT_RHYTHM = 0.25f;
        private const float WEIGHT_SECTION = 0.15f;
        private const float WEIGHT_TECHNIQUE = 0.10f;
        private const float WEIGHT_PLAYABILITY = 0.10f;
        private const float WEIGHT_REPETITION = 0.05f;

        public NoteFilter(SongInstrumentNotes notes, SongStructure structure)
        {
            originalNotes = notes;
            beats = structure?.Beats ?? new List<SongBeat>();
            sections = notes?.Sections ?? new List<SongSection>();
            noteImportanceScores = new Dictionary<int, float>();

            // Build beat grid for quick lookup
            beatGrid = beats.Select(b => (b.TimeOffset, b.IsMeasure)).ToList();

            // Pre-calculate importance scores for all notes
            if (originalNotes?.Notes != null)
            {
                for (int i = 0; i < originalNotes.Notes.Count; i++)
                {
                    noteImportanceScores[i] = CalculateNoteImportance(originalNotes.Notes[i], i);
                }
            }
        }

        public List<SongNote> GetFilteredNotes()
        {
            if (DifficultyPercent >= 100.0f || originalNotes?.Notes == null || originalNotes.Notes.Count == 0)
                return originalNotes?.Notes ?? new List<SongNote>();

            // Get difficulty rules
            var tier = GetDifficultyTier(DifficultyPercent);
            var rules = GetDifficultyRules(DifficultyPercent);

            // Calculate retention ratio using curved function
            float retentionRatio = DifficultyToRetentionRatio(DifficultyPercent);

            // Select notes based on importance and rules
            var selectedNotes = SelectNotesForDifficulty(retentionRatio, rules);

            // Ensure musical coherence
            selectedNotes = EnsureMusicalCoherence(selectedNotes, rules);

            // Optimize for playability
            selectedNotes = OptimizePlayability(selectedNotes, rules);

            return selectedNotes;
        }

        /// <summary>
        /// Non-linear difficulty to retention ratio curve
        /// Provides more granularity at lower difficulties
        /// </summary>
        private float DifficultyToRetentionRatio(float difficultyPercent)
        {
            float d = difficultyPercent / 100.0f;

            // More aggressive piecewise function for lower difficulties
            if (d <= 0.10f)
            {
                // Very easy: retain only 2-5% of notes (just key downbeats)
                return 0.02f + (d / 0.10f) * 0.03f;
            }
            else if (d <= 0.30f)
            {
                // Easy: retain 5-15% of notes
                return 0.05f + ((d - 0.10f) / 0.20f) * 0.10f;
            }
            else if (d <= 0.60f)
            {
                // Moderate: retain 15-40% of notes
                return 0.15f + ((d - 0.30f) / 0.30f) * 0.25f;
            }
            else if (d <= 0.80f)
            {
                // Advanced: retain 40-70% of notes
                return 0.40f + ((d - 0.60f) / 0.20f) * 0.30f;
            }
            else
            {
                // Expert: retain 70-100% of notes
                return 0.70f + ((d - 0.80f) / 0.20f) * 0.30f;
            }
        }

        private DifficultyTier GetDifficultyTier(float difficultyPercent)
        {
            if (difficultyPercent <= 20)
                return new DifficultyTier
                {
                    Name = "Beginner",
                    MinPercent = 1, MaxPercent = 20,
                    MaxNotesPerBeat = 2,
                    MinNoteDuration = 0.5f,
                    MaxFretSpan = 3,
                    MaxSimultaneousNotes = 1,
                    AllowedTechniques = new HashSet<ESongNoteTechnique> { 0, ESongNoteTechnique.PalmMute },
                    AllowPositionShifts = false,
                    AllowStringSkipping = false
                };
            else if (difficultyPercent <= 40)
                return new DifficultyTier
                {
                    Name = "Easy",
                    MinPercent = 21, MaxPercent = 40,
                    MaxNotesPerBeat = 4,
                    MinNoteDuration = 0.25f,
                    MaxFretSpan = 4,
                    MaxSimultaneousNotes = 2,
                    AllowedTechniques = new HashSet<ESongNoteTechnique> { 0, ESongNoteTechnique.PalmMute, ESongNoteTechnique.Slide },
                    AllowPositionShifts = true,
                    AllowStringSkipping = false
                };
            else if (difficultyPercent <= 60)
                return new DifficultyTier
                {
                    Name = "Intermediate",
                    MinPercent = 41, MaxPercent = 60,
                    MaxNotesPerBeat = 8,
                    MinNoteDuration = 0.125f,
                    MaxFretSpan = 5,
                    MaxSimultaneousNotes = 4,
                    AllowedTechniques = new HashSet<ESongNoteTechnique> { 0, ESongNoteTechnique.PalmMute, ESongNoteTechnique.Slide,
                        ESongNoteTechnique.HammerOn, ESongNoteTechnique.PullOff, ESongNoteTechnique.Bend, ESongNoteTechnique.Vibrato },
                    AllowPositionShifts = true,
                    AllowStringSkipping = true
                };
            else if (difficultyPercent <= 80)
                return new DifficultyTier
                {
                    Name = "Advanced",
                    MinPercent = 61, MaxPercent = 80,
                    MaxNotesPerBeat = 16,
                    MinNoteDuration = 0.0625f,
                    MaxFretSpan = 5,
                    MaxSimultaneousNotes = 5,
                    AllowedTechniques = new HashSet<ESongNoteTechnique> { 0, ESongNoteTechnique.PalmMute, ESongNoteTechnique.Slide,
                        ESongNoteTechnique.HammerOn, ESongNoteTechnique.PullOff, ESongNoteTechnique.Bend, ESongNoteTechnique.Vibrato,
                        ESongNoteTechnique.Harmonic },
                    AllowPositionShifts = true,
                    AllowStringSkipping = true
                };
            else
                return new DifficultyTier
                {
                    Name = "Expert",
                    MinPercent = 81, MaxPercent = 100,
                    MaxNotesPerBeat = 32,
                    MinNoteDuration = 0.0f,
                    MaxFretSpan = 6,
                    MaxSimultaneousNotes = 6,
                    AllowedTechniques = null, // All allowed
                    AllowPositionShifts = true,
                    AllowStringSkipping = true
                };
        }

        private DifficultyTier GetDifficultyRules(float difficultyPercent)
        {
            return GetDifficultyTier(difficultyPercent);
        }

        /// <summary>
        /// Calculate comprehensive importance score for a note (0.0 - 1.0)
        /// </summary>
        private float CalculateNoteImportance(SongNote note, int noteIndex)
        {
            float score = 0.0f;

            // 1. Musical Function Score (35%)
            NoteFunction function = ClassifyNote(note, noteIndex);
            float functionScore = (float)function / 100.0f;
            score += functionScore * WEIGHT_FUNCTION;

            // 2. Rhythmic Position Score (25%)
            float rhythmScore = GetBeatStrength(note.TimeOffset);
            score += rhythmScore * WEIGHT_RHYTHM;

            // 3. Section Importance Score (15%)
            float sectionScore = GetSectionImportance(note.TimeOffset);
            score += sectionScore * WEIGHT_SECTION;

            // 4. Technique Score (10%) - simpler techniques slightly preferred at lower difficulties
            float techniqueScore = GetTechniqueScore(note.Techniques);
            score += techniqueScore * WEIGHT_TECHNIQUE;

            // 5. Playability Score (10%)
            float playabilityScore = GetPlayabilityScore(note);
            score += playabilityScore * WEIGHT_PLAYABILITY;

            // 6. Repetition bonus (5%) - notes in patterns are more learnable
            float repetitionScore = GetRepetitionScore(note, noteIndex);
            score += repetitionScore * WEIGHT_REPETITION;

            return Math.Min(1.0f, score);
        }

        /// <summary>
        /// Classify a note by its musical function
        /// </summary>
        private NoteFunction ClassifyNote(SongNote note, int noteIndex)
        {
            // Check for chord relationship
            if (note.ChordID != -1 && originalNotes.Chords != null && note.ChordID < originalNotes.Chords.Count)
            {
                var chord = originalNotes.Chords[note.ChordID];

                // Check if this is the root (lowest fretted string in chord)
                if (IsChordRoot(note, chord))
                    return NoteFunction.RootNote;

                // Check for chord note flag
                if (note.Techniques.HasFlag(ESongNoteTechnique.ChordNote))
                    return NoteFunction.ChordVoicing;

                // Main chord marker
                if (note.Techniques.HasFlag(ESongNoteTechnique.Chord))
                    return NoteFunction.ChordFifth;
            }

            // Check rhythmic position - downbeats are anchors
            float beatStrength = GetBeatStrength(note.TimeOffset);
            if (beatStrength >= 0.95f)
                return NoteFunction.DownbeatAnchor;

            // Check for ornamental characteristics
            if (note.TimeLength < 0.1f)
            {
                if (note.Techniques.HasFlag(ESongNoteTechnique.HammerOn) ||
                    note.Techniques.HasFlag(ESongNoteTechnique.PullOff))
                    return NoteFunction.GraceNote;
                return NoteFunction.Ornament;
            }

            // Check for advanced techniques
            if (note.Techniques.HasFlag(ESongNoteTechnique.Harmonic) ||
                note.Techniques.HasFlag(ESongNoteTechnique.PinchHarmonic))
                return NoteFunction.Ornament;

            if (note.Techniques.HasFlag(ESongNoteTechnique.Tap))
                return NoteFunction.Ornament;

            // Check for accent (important note)
            if (note.Techniques.HasFlag(ESongNoteTechnique.Accent))
                return NoteFunction.MelodyPrimary;

            // Strong beat notes are rhythmic anchors
            if (beatStrength >= 0.7f)
                return NoteFunction.RhythmicFill;

            // Check for passing tones (short notes between beats)
            if (beatStrength < 0.4f && note.TimeLength < 0.25f)
                return NoteFunction.PassingTone;

            // Default classification
            return NoteFunction.RhythmicFill;
        }

        /// <summary>
        /// Get beat strength at a time position (1.0 = downbeat, lower = weaker)
        /// </summary>
        private float GetBeatStrength(float timeOffset)
        {
            const float tolerance = 0.05f;

            // Check for measure start (strongest)
            foreach (var beat in beatGrid)
            {
                if (beat.isMeasure && Math.Abs(beat.time - timeOffset) < tolerance)
                    return 1.0f;
            }

            // Check for any beat
            foreach (var beat in beatGrid)
            {
                float diff = Math.Abs(beat.time - timeOffset);
                if (diff < tolerance)
                    return 0.8f;
                if (diff < tolerance * 2)
                    return 0.6f;
            }

            // Off-beat
            return 0.3f;
        }

        /// <summary>
        /// Get section importance (chorus > verse > etc.)
        /// </summary>
        private float GetSectionImportance(float timeOffset)
        {
            foreach (var section in sections)
            {
                if (timeOffset >= section.StartTime && timeOffset < section.EndTime)
                {
                    string name = section.Name?.ToLower() ?? "";

                    if (name.Contains("chorus") || name.Contains("hook"))
                        return 1.0f;
                    if (name.Contains("riff") || name.Contains("main"))
                        return 0.95f;
                    if (name.Contains("solo"))
                        return 0.9f;
                    if (name.Contains("intro"))
                        return 0.85f;
                    if (name.Contains("verse"))
                        return 0.7f;
                    if (name.Contains("bridge"))
                        return 0.65f;
                    if (name.Contains("outro") || name.Contains("ending"))
                        return 0.5f;
                    if (name.Contains("breakdown") || name.Contains("quiet"))
                        return 0.4f;
                }
            }
            return 0.6f; // Default
        }

        /// <summary>
        /// Score technique complexity (simpler = higher at low difficulties)
        /// </summary>
        private float GetTechniqueScore(ESongNoteTechnique techniques)
        {
            // No technique = most accessible
            if (techniques == 0)
                return 0.9f;

            float score = 0.5f;

            // Simple techniques
            if (techniques.HasFlag(ESongNoteTechnique.PalmMute))
                score = Math.Max(score, 0.7f);

            // Moderate techniques
            if (techniques.HasFlag(ESongNoteTechnique.Slide))
                score = Math.Min(score, 0.6f);
            if (techniques.HasFlag(ESongNoteTechnique.HammerOn) || techniques.HasFlag(ESongNoteTechnique.PullOff))
                score = Math.Min(score, 0.55f);

            // Complex techniques
            if (techniques.HasFlag(ESongNoteTechnique.Bend))
                score = Math.Min(score, 0.4f);
            if (techniques.HasFlag(ESongNoteTechnique.Vibrato))
                score = Math.Min(score, 0.45f);

            // Advanced techniques
            if (techniques.HasFlag(ESongNoteTechnique.Harmonic) || techniques.HasFlag(ESongNoteTechnique.PinchHarmonic))
                score = Math.Min(score, 0.25f);
            if (techniques.HasFlag(ESongNoteTechnique.Tap))
                score = Math.Min(score, 0.2f);
            if (techniques.HasFlag(ESongNoteTechnique.Tremolo))
                score = Math.Min(score, 0.3f);

            return score;
        }

        /// <summary>
        /// Score playability (open strings and lower frets preferred)
        /// </summary>
        private float GetPlayabilityScore(SongNote note)
        {
            float score = 0.5f;

            // Open string bonus
            if (note.Fret == 0)
                score += 0.3f;
            // Lower frets easier
            else if (note.Fret <= 5)
                score += 0.2f;
            else if (note.Fret <= 12)
                score += 0.1f;
            // High frets harder
            else if (note.Fret > 12)
                score -= 0.1f;

            return Math.Max(0, Math.Min(1, score));
        }

        /// <summary>
        /// Repetition bonus for learnable patterns
        /// </summary>
        private float GetRepetitionScore(SongNote note, int noteIndex)
        {
            // Simple heuristic: notes at similar positions tend to repeat
            // Check for similar hand position patterns
            int similarCount = 0;
            for (int i = 0; i < originalNotes.Notes.Count; i++)
            {
                if (i == noteIndex) continue;
                var other = originalNotes.Notes[i];
                if (other.HandFret == note.HandFret && other.String == note.String && other.Fret == note.Fret)
                    similarCount++;
            }

            return Math.Min(1.0f, similarCount / 8.0f);
        }

        /// <summary>
        /// Check if note is the root of its chord
        /// </summary>
        private bool IsChordRoot(SongNote note, SongChord chord)
        {
            if (chord?.Frets == null || chord.Frets.Count == 0)
                return false;

            // Root is typically the lowest fretted string (highest string number with fret >= 0)
            int rootString = -1;
            for (int i = chord.Frets.Count - 1; i >= 0; i--)
            {
                if (chord.Frets[i] >= 0)
                {
                    rootString = i;
                    break;
                }
            }

            return note.String == rootString;
        }

        /// <summary>
        /// Select notes based on importance scores and retention ratio
        /// </summary>
        private List<SongNote> SelectNotesForDifficulty(float retentionRatio, DifficultyTier rules)
        {
            if (originalNotes?.Notes == null || originalNotes.Notes.Count == 0)
                return new List<SongNote>();

            // Create indexed list with scores
            var scoredNotes = new List<(SongNote note, float score, int index)>();
            for (int i = 0; i < originalNotes.Notes.Count; i++)
            {
                scoredNotes.Add((originalNotes.Notes[i], noteImportanceScores[i], i));
            }

            // Sort by score descending
            scoredNotes.Sort((a, b) => b.score.CompareTo(a.score));

            // Calculate target count
            int targetCount = Math.Max(1, (int)(scoredNotes.Count * retentionRatio));

            // Select top notes that pass difficulty rules
            var selected = new List<(SongNote note, int index)>();
            var notesByBeat = new Dictionary<int, List<SongNote>>();

            foreach (var (note, score, index) in scoredNotes)
            {
                if (selected.Count >= targetCount)
                    break;

                if (PassesDifficultyRules(note, rules, selected.Select(s => s.note).ToList(), notesByBeat))
                {
                    selected.Add((note, index));

                    // Track notes per beat
                    int beatIndex = (int)(note.TimeOffset * 4); // Quarter beat granularity
                    if (!notesByBeat.ContainsKey(beatIndex))
                        notesByBeat[beatIndex] = new List<SongNote>();
                    notesByBeat[beatIndex].Add(note);
                }
            }

            // Sort back to time order
            selected.Sort((a, b) => a.index.CompareTo(b.index));

            return selected.Select(s => s.note).ToList();
        }

        /// <summary>
        /// Check if a note passes difficulty-based rules
        /// </summary>
        private bool PassesDifficultyRules(SongNote note, DifficultyTier rules, List<SongNote> alreadySelected, Dictionary<int, List<SongNote>> notesByBeat)
        {
            // Check technique allowance (null = all allowed)
            if (rules.AllowedTechniques != null)
            {
                // Check each technique flag
                foreach (ESongNoteTechnique tech in Enum.GetValues(typeof(ESongNoteTechnique)))
                {
                    if (tech != 0 && note.Techniques.HasFlag(tech) && !rules.AllowedTechniques.Contains(tech))
                    {
                        // Technique not allowed, but if note is very important, keep it
                        int noteIdx = originalNotes.Notes.IndexOf(note);
                        if (noteIdx >= 0 && noteImportanceScores.ContainsKey(noteIdx) && noteImportanceScores[noteIdx] < 0.8f)
                            return false;
                    }
                }
            }

            // Check minimum duration
            if (note.TimeLength > 0 && note.TimeLength < rules.MinNoteDuration)
                return false;

            // Check simultaneous notes limit
            var simultaneous = alreadySelected.Where(n => Math.Abs(n.TimeOffset - note.TimeOffset) < 0.02f).ToList();
            if (simultaneous.Count >= rules.MaxSimultaneousNotes)
                return false;

            // Check fret span
            if (simultaneous.Any())
            {
                var frets = simultaneous.Where(n => n.Fret > 0).Select(n => n.Fret).ToList();
                if (note.Fret > 0)
                    frets.Add(note.Fret);

                if (frets.Any() && frets.Max() - frets.Min() > rules.MaxFretSpan)
                    return false;
            }

            // Check notes per beat limit
            int beatIndex = (int)(note.TimeOffset * 4);
            if (notesByBeat.ContainsKey(beatIndex) && notesByBeat[beatIndex].Count >= rules.MaxNotesPerBeat)
                return false;

            return true;
        }

        /// <summary>
        /// Ensure musical coherence - no large gaps, structural notes present
        /// Limited by difficulty to avoid undoing the filtering
        /// </summary>
        private List<SongNote> EnsureMusicalCoherence(List<SongNote> selected, DifficultyTier rules)
        {
            if (selected.Count == 0 || originalNotes?.Notes == null)
                return selected;

            // At very low difficulties, skip coherence entirely to keep it sparse
            if (DifficultyPercent <= 15)
                return selected;

            var result = new List<SongNote>(selected);
            var selectedSet = new HashSet<float>(selected.Select(n => n.TimeOffset));

            // Limit how many notes we can add back (max 15% of original selection)
            int maxAdditions = Math.Max(2, (int)(selected.Count * 0.15f));
            int additions = 0;

            // 1. Ensure no large gaps - scale gap tolerance with difficulty
            float maxGap = DifficultyPercent <= 30 ? 4.0f : (DifficultyPercent <= 50 ? 3.0f : 2.0f);
            var sorted = result.OrderBy(n => n.TimeOffset).ToList();

            for (int i = 0; i < sorted.Count - 1 && additions < maxAdditions; i++)
            {
                float gap = sorted[i + 1].TimeOffset - sorted[i].TimeOffset;
                if (gap > maxGap)
                {
                    // Find a note to fill the gap
                    float midTime = sorted[i].TimeOffset + gap / 2;
                    var fillNote = FindBestFillNote(midTime, selectedSet);
                    if (fillNote.HasValue)
                    {
                        result.Add(fillNote.Value);
                        selectedSet.Add(fillNote.Value.TimeOffset);
                        additions++;
                    }
                }
            }

            // 2. At medium+ difficulty, ensure downbeats have notes (at measure boundaries)
            if (DifficultyPercent >= 40)
            {
                foreach (var beat in beatGrid.Where(b => b.isMeasure))
                {
                    if (additions >= maxAdditions) break;
                    bool hasNote = result.Any(n => Math.Abs(n.TimeOffset - beat.time) < 0.1f);
                    if (!hasNote)
                    {
                        var downbeatNote = FindBestNoteAtTime(beat.time, selectedSet);
                        if (downbeatNote.HasValue)
                        {
                            result.Add(downbeatNote.Value);
                            selectedSet.Add(downbeatNote.Value.TimeOffset);
                            additions++;
                        }
                    }
                }
            }

            // 3. At higher difficulties, ensure section starts have notes
            if (DifficultyPercent >= 50)
            {
                foreach (var section in sections)
                {
                    if (additions >= maxAdditions) break;
                    bool hasNote = result.Any(n => Math.Abs(n.TimeOffset - section.StartTime) < 0.2f);
                    if (!hasNote)
                    {
                        var sectionNote = FindBestNoteAtTime(section.StartTime, selectedSet);
                        if (sectionNote.HasValue)
                        {
                            result.Add(sectionNote.Value);
                            selectedSet.Add(sectionNote.Value.TimeOffset);
                            additions++;
                        }
                    }
                }
            }

            return result.OrderBy(n => n.TimeOffset).ToList();
        }

        /// <summary>
        /// Find the best note to fill a gap
        /// </summary>
        private SongNote? FindBestFillNote(float targetTime, HashSet<float> alreadySelected)
        {
            float tolerance = 0.5f;
            SongNote? best = null;
            float bestScore = -1;

            foreach (var note in originalNotes.Notes)
            {
                if (Math.Abs(note.TimeOffset - targetTime) <= tolerance && !alreadySelected.Contains(note.TimeOffset))
                {
                    int idx = originalNotes.Notes.IndexOf(note);
                    if (idx >= 0 && noteImportanceScores.ContainsKey(idx))
                    {
                        float score = noteImportanceScores[idx];
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = note;
                        }
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Find the best note at a specific time
        /// </summary>
        private SongNote? FindBestNoteAtTime(float targetTime, HashSet<float> alreadySelected)
        {
            float tolerance = 0.2f;
            SongNote? best = null;
            float bestScore = -1;

            foreach (var note in originalNotes.Notes)
            {
                if (Math.Abs(note.TimeOffset - targetTime) <= tolerance && !alreadySelected.Contains(note.TimeOffset))
                {
                    int idx = originalNotes.Notes.IndexOf(note);
                    if (idx >= 0 && noteImportanceScores.ContainsKey(idx))
                    {
                        float score = noteImportanceScores[idx];
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = note;
                        }
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Optimize for playability - reduce awkward stretches
        /// </summary>
        private List<SongNote> OptimizePlayability(List<SongNote> selected, DifficultyTier rules)
        {
            if (!rules.AllowPositionShifts || selected.Count < 2)
                return selected;

            var result = new List<SongNote>();
            var simultaneousGroups = GroupSimultaneousNotes(selected);

            foreach (var group in simultaneousGroups)
            {
                if (group.Count <= 1)
                {
                    result.AddRange(group);
                    continue;
                }

                // Check fret span
                var frettedNotes = group.Where(n => n.Fret > 0).ToList();
                if (frettedNotes.Count > 1)
                {
                    int span = frettedNotes.Max(n => n.Fret) - frettedNotes.Min(n => n.Fret);
                    if (span > rules.MaxFretSpan)
                    {
                        // Keep notes closest to center position
                        float centerFret = (float)frettedNotes.Average(n => n.Fret);
                        var sorted = group.OrderBy(n => n.Fret > 0 ? Math.Abs(n.Fret - centerFret) : 0).ToList();

                        // Keep notes that fit within span
                        var kept = new List<SongNote>();
                        foreach (var note in sorted)
                        {
                            if (note.Fret == 0)
                            {
                                kept.Add(note);
                                continue;
                            }

                            var testFrets = kept.Where(n => n.Fret > 0).Select(n => n.Fret).Concat(new[] { note.Fret }).ToList();
                            if (testFrets.Max() - testFrets.Min() <= rules.MaxFretSpan)
                            {
                                kept.Add(note);
                            }
                        }
                        result.AddRange(kept);
                    }
                    else
                    {
                        result.AddRange(group);
                    }
                }
                else
                {
                    result.AddRange(group);
                }
            }

            return result.OrderBy(n => n.TimeOffset).ToList();
        }

        /// <summary>
        /// Group notes that are played simultaneously
        /// </summary>
        private List<List<SongNote>> GroupSimultaneousNotes(List<SongNote> notes)
        {
            var groups = new List<List<SongNote>>();
            var sorted = notes.OrderBy(n => n.TimeOffset).ToList();

            List<SongNote> currentGroup = null;
            float lastTime = -1;

            foreach (var note in sorted)
            {
                if (currentGroup == null || note.TimeOffset - lastTime > 0.02f)
                {
                    currentGroup = new List<SongNote>();
                    groups.Add(currentGroup);
                }
                currentGroup.Add(note);
                lastTime = note.TimeOffset;
            }

            return groups;
        }
    }
}
