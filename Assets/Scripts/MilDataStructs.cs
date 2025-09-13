using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections.Generic;

using Utils;
using MilPlayment;
using MilConst;

namespace MilDataStructs {
    [Serializable]
    public class ChartMeta {
        public string chart_file;
        public string audio_file;
        public string image_file;
    }

    [Serializable]
    public class MilChart {
        public int fmt;
        public MilMeta meta;
        public List<MilBpmItem> bpms;
        public List<MilJudgeLine> lines;
        public List<MilStoryBoard> storyboards;

        public List<double> comboTimes;
        public MilPlayment.MilPlayment playment;
        public AnimUtils.ValueTransformer comboScaleValueTrans;
        private int last_combo;
        private double comboScaleLastChange;

        void InitAnimations(List<MilAnimation> es) {
            foreach (var e in es) {
                e.timeSec = Beat2Sec(MilChartUtils.BeatToNumber(e.startTime));
                e.endTimeSec = Beat2Sec(MilChartUtils.BeatToNumber(e.endTime));

                if (e.ease.isValueExp) {
                    e.start = 0.0;
                    e.end = 1.0;
                }
            }
        }

        public void Init() {
            comboTimes = new List<double>();
            comboScaleValueTrans = new AnimUtils.ValueTransformer(x => (1.0 - Math.Pow(1.0 - x, 3)), 0.08);
            last_combo = -1;
            
            foreach (var bpm in bpms) {
                bpm.timeSec = Beat2Sec(MilChartUtils.BeatToNumber(bpm.time));
            }

            var noteTimeMap = new Dictionary<double, int>();

            foreach (var line in lines) {
                line.Init();
                line.animationCollection = new MilAnimationCollection();
                line.animationCollection.collectionType = (int)MilAnimationCollectionType.Line;
                line.animationCollection.Init();
                InitAnimations(line.animations);

                var notesMap = new Dictionary<int, MilNote>();

                foreach (var note in line.notes) {
                    note.Init();
                    note.master = line;
                    note.timeSec = Beat2Sec(MilChartUtils.BeatToNumber(note.time));
                    note.endTimeSec = Beat2Sec(MilChartUtils.BeatToNumber(note.endTime));
                    note.animationCollection = new MilAnimationCollection();
                    note.animationCollection.collectionType = (int)MilAnimationCollectionType.Note;
                    note.animationCollection.Init();
                    notesMap[note.index] = note;

                    comboTimes.Add(note.timeSec);
                    if (note.isHold) comboTimes.Add(note.endTimeSec);

                    if (!noteTimeMap.ContainsKey(note.timeSec)) noteTimeMap[note.timeSec] = 0;
                    noteTimeMap[note.timeSec]++;
                }

                foreach (var e in line.animations) {
                    if (e.bearer_type == (int)MilAnimationBearerType.Line && e.bearer == line.index) {
                        line.animationCollection.animations[e.type].Add(e);
                    } else if (e.bearer_type == (int)MilAnimationBearerType.Note && notesMap.ContainsKey(e.bearer)) {
                        MilNote note = notesMap[e.bearer];
                        note.animationCollection.animations[e.type].Add(e);
                    }
                }

                line.animationCollection.AfterLoad();
                foreach (var note in line.notes) {
                    note.animationCollection.AfterLoad();
                    line.animationCollection.Update(note.timeSec, (int)MilAnimationType.Speed);
                    note.floorPosition = line.animationCollection.GetValue((int)MilAnimationType.Speed);
                    line.animationCollection.Update(note.endTimeSec, (int)MilAnimationType.Speed);
                    note.endFloorPosition = line.animationCollection.GetValue((int)MilAnimationType.Speed);
                    note.InitProperties();
                }
            }

            foreach (var line in lines) {
                foreach (var note in line.notes) {
                    note.isMorebets = noteTimeMap[note.timeSec] > 1;
                }
            }

            foreach (var storyboard in storyboards) {
                storyboard.animationCollection = new MilAnimationCollection();
                storyboard.animationCollection.collectionType = (int)MilAnimationCollectionType.StoryBoard;
                storyboard.animationCollection.Init();
                InitAnimations(storyboard.animations);

                foreach (var e in storyboard.animations) {
                    if (e.bearer_type == (int)MilAnimationBearerType.StoryBoard && e.bearer == storyboard.index) {
                        storyboard.animationCollection.animations[e.type].Add(e);
                    }
                }

                storyboard.animationCollection.AfterLoad();
            }

            comboTimes.Sort();
        }

        public void OnComboUpdated(int combo) {
            if (combo == last_combo) return;
            last_combo = combo;

            comboScaleValueTrans.target = 1.05;
            comboScaleLastChange = comboScaleValueTrans.time_getter();
        }

        public void TryresetComboScale() {
            if (comboScaleValueTrans.target != 1.0 && comboScaleValueTrans.time_getter() - comboScaleLastChange > comboScaleValueTrans.animation_time) {
                comboScaleValueTrans.target = 1.0;
            }
        }

        public double Beat2Sec(double t) {
            double sec = meta.offset;
            for (int i = 0; i < bpms.Count; i++) {
                MilBpmItem bpm = bpms[i];
                if (i != bpms.Count - 1) {
                    double et_beat = MilChartUtils.BeatToNumber(bpms[i + 1].time) - MilChartUtils.BeatToNumber(bpm.time);

                    if (t >= et_beat) {
                        sec += et_beat * (60 / bpm.bpm);
                        t -= et_beat;
                    } else {
                        sec += t * (60 / bpm.bpm);
                        break;
                    }
                } else {
                    sec += t * (60 / bpm.bpm);
                }
            }
            return sec;
        }

        public void Update(double t) {
            foreach (var line in lines) {
                line.animationCollection.Update(t);

                foreach (var note in line.notes) {
                    note.animationCollection.Update(t);
                }
            }

            foreach (var storyboard in storyboards) {
                storyboard.animationCollection.Update(t);
            }
        }

        public int GetCombo(double t) {
            var l = comboTimes.BinarySearch(t);
            if (l < 0) l = ~l;
            if (l == 0) return 0;
            return l;
        }
    }

    [Serializable]
    public class MilMeta {
        public double background_dim;
        public string name;
        public string background_artist;
        public string music_artist;
        public string charter;
        public string difficulty_name;
        public double difficulty;
        public double offset;
    }

    [Serializable]
    public class MilBpmItem {
        public double[] time;
        public double bpm;

        public double timeSec;
    }

    [Serializable]
    public class MilJudgeLine {
        public int index;
        public List<MilNote> notes;
        public List<MilAnimation> animations;

        public MilAnimationCollection animationCollection;
        public GameObject linePrefab;
        public GameObject lineHead;
        public GameObject lineBody;
        public ObjectPool<GameObject> notePool;
        public ObjectPool<GameObject> holdNotePool;
        public ObjectPool<GameObject> hitParticlePool;
        public ObjectPool<GameObject> holdHitParticlePool;
        public ObjectPool<GameObject> goodHitParticlePool;
        public ObjectPool<GameObject> goodHoldHitParticlePool;
        public HashSet<GameObject> activeHitParticles;
        public Vector2 currentCanvasPosition;

        public void Init() {
            animationCollection = null;
            linePrefab = null;
            lineHead = null;
            lineBody = null;
            notePool = null;
            holdNotePool = null;
            hitParticlePool = null;
            holdHitParticlePool = null;
            goodHitParticlePool = null;
            goodHoldHitParticlePool = null;
            activeHitParticles = new HashSet<GameObject>();
            currentCanvasPosition = Vector2.zero;
        }
    }

    [Serializable]
    public class MilNote {
        public int index;
        public double[] time;
        public double[] endTime;
        public int type;
        public bool isFake;
        public bool isAlwaysPerfect;

        [NonSerialized]
        public MilJudgeLine master;
        public double timeSec;
        public double endTimeSec;
        public bool clicked;
        public bool isMorebets;
        public double floorPosition;
        public double endFloorPosition;
        public double realScale;
        public MilAnimationCollection animationCollection;
        public GameObject notePrefab;
        public double particleStarttime;
        public bool hitParticlePlayed;
        public double hitParticleOffset;
        public GameObject hitParticle;
        public GameObject holdHitParticle;
        public GameObject[] notePolyDisplayPrefab;

        public int judge_state;
        public double judge_time;
        public bool judge_hited;
        public bool judge_ismiss;
        public double judge_misstime;
        public bool head_judged;
        public bool judge_drag_prejudge;
        public double prejudge_time;
        public double judge_holdlastcheck;
        public HashSet<MilPlayment.MilPlayment.Touch> prejudge_touches;
        public bool judge_gived_endcombo;
        public bool judge_isgood;

        public bool isHold => type == (int)MilNoteType.Hit && endTimeSec > timeSec;

        public void Init() {
            master = null;
            timeSec = 0.0;
            endTimeSec = 0.0;
            clicked = false;
            isMorebets = false;
            floorPosition = 0.0;
            endFloorPosition = 0.0;
            realScale = 0.0;
            animationCollection = null;
            notePrefab = null;
            particleStarttime = 0.0;
            hitParticlePlayed = false;
            hitParticleOffset = 0.0;
            hitParticle = null;
            holdHitParticle = null;
            notePolyDisplayPrefab = null;

            judge_state = 0;
            judge_time = 0.0;
            judge_hited = false;
            judge_ismiss = false;
            judge_misstime = 0.0;
            head_judged = false;
            judge_drag_prejudge = false;
            prejudge_time = 0.0;
            judge_holdlastcheck = 0.0;
            prejudge_touches = null;
            judge_gived_endcombo = false;
            judge_isgood = false;
        }
        
        public void InitProperties() {
            particleStarttime = timeSec;

            judge_state = (int)EnumJudgeState.Miss;
            judge_time = timeSec + Math.Max(JudgeRange.Bad * 2, MilConst.MilConst.PLAY_NOTE_DISAPPEAR_TIME);
            judge_hited = false;
            judge_ismiss = false;
            judge_misstime = endTimeSec;
            head_judged = false;
            judge_drag_prejudge = false;
            prejudge_time = 0.0;
            judge_holdlastcheck = 0.0;
            prejudge_touches = new HashSet<MilPlayment.MilPlayment.Touch>();
            judge_gived_endcombo = false;
            judge_isgood = false;
        }

        public Vector2 GetPosition(
            double t,
            double noteFp,
            Func<double, double> TransformX,
            Func<double, double> TransformY
        ) {
            var noteCenter = new Vector2(
                0, (isHold && timeSec <= t) ? 0 : (float)noteFp
            );

            if (animationCollection.HasEvents((int)MilAnimationType.PositionX)) noteCenter.x = (float)TransformX(animationCollection.GetValue((int)MilAnimationType.PositionX));
            if (animationCollection.HasEvents((int)MilAnimationType.PositionY)) noteCenter.y = (float)TransformY(animationCollection.GetValue((int)MilAnimationType.PositionY));
            noteCenter.x += (float)animationCollection.GetValue((int)MilAnimationType.RelativeX);
            noteCenter.y += (float)animationCollection.GetValue((int)MilAnimationType.RelativeY);

            return noteCenter;
        }

        public MathUtils.WebCanvas2DTransform GetCanvasTransform(
            Vector2 lineCenter, 
            double lineRotation,
            double lineSize,
            double noteScale,
            double noteRotation
        ) {
            var transform = new MathUtils.WebCanvas2DTransform();
            transform.translate((double)lineCenter.x, (double)lineCenter.y);
            transform.scale(lineSize, lineSize);
            transform.rotateDegree(90.0 - lineRotation);
            transform.scale(noteScale, noteScale);
            transform.rotateDegree(noteRotation);
            return transform;
        }
    }

    [Serializable]
    public class MilAnimation {
        public int index;
        public int type;
        public double[] startTime;
        public double[] endTime;
        public double start;
        public double end;
        public int bearer_type;
        public int bearer;
        public MilAnimEase ease;

        public double timeSec;
        public double endTimeSec;
        public double floorPosition;
    }

    [Serializable]
    public class MilAnimEase {
        public int type;
        public int press;
        public bool isValueExp;
        public string cusValueExp;
        public double clipLeft;
        public double clipRight;

        double easing_in(int press, double p) {
            switch (press) {
                case 0: return p;
                case 1: return (1.0 - Math.Cos(((p * Math.PI) / 2.0)));
                case 2: return Math.Pow(p, 2.0);
                case 3: return Math.Pow(p, 3.0);
                case 4: return Math.Pow(p, 4.0);
                case 5: return Math.Pow(p, 5.0);
                case 6: return ((p == 0.0) ? 0.0 : Math.Pow(2.0, ((10.0 * p) - 10.0)));
                case 7: return (1.0 - Math.Pow((1.0 - Math.Pow(p, 2.0)), 0.5));
                case 8: return ((2.70158 * Math.Pow(p, 3.0)) - (1.70158 * Math.Pow(p, 2.0)));
                case 9: return ((p == 0.0) ? 0.0 : ((p == 1.0) ? 1.0 : ((-Math.Pow(2.0, ((10.0 * p) - 10.0))) * Math.Sin((((p * 10.0) - 10.75) * ((2.0 * Math.PI) / 3.0))))));
                case 10: return (1.0 - (((1.0 - p) < (1.0 / 2.75)) ? (7.5625 * Math.Pow((1.0 - p), 2.0)) : (((1.0 - p) < (2.0 / 2.75)) ? (((7.5625 * ((1.0 - p) - (1.5 / 2.75))) * ((1.0 - p) - (1.5 / 2.75))) + 0.75) : (((1.0 - p) < (2.5 / 2.75)) ? (((7.5625 * ((1.0 - p) - (2.25 / 2.75))) * ((1.0 - p) - (2.25 / 2.75))) + 0.9375) : (((7.5625 * ((1.0 - p) - (2.625 / 2.75))) * ((1.0 - p) - (2.625 / 2.75))) + 0.984375)))));
                default: return p;
            }
        }

        double easing_out(int press, double p) {
            switch (press) {
                case 0: return p;
                case 1: return Math.Sin(((p * Math.PI) / 2.0));
                case 2: return (1.0 - ((1.0 - p) * (1.0 - p)));
                case 3: return (1.0 - Math.Pow((1.0 - p), 3.0));
                case 4: return (1.0 - Math.Pow((1.0 - p), 4.0));
                case 5: return (1.0 - Math.Pow((1.0 - p), 5.0));
                case 6: return ((p == 1.0) ? 1.0 : (1.0 - Math.Pow(2.0, ((-10.0) * p))));
                case 7: return Math.Pow((1.0 - Math.Pow((p - 1.0), 2.0)), 0.5);
                case 8: return ((1.0 + (2.70158 * Math.Pow((p - 1.0), 3.0))) + (1.70158 * Math.Pow((p - 1.0), 2.0)));
                case 9: return ((p == 0.0) ? 0.0 : ((p == 1.0) ? 1.0 : ((Math.Pow(2.0, ((-10.0) * p)) * Math.Sin((((p * 10.0) - 0.75) * ((2.0 * Math.PI) / 3.0)))) + 1.0)));
                case 10: return ((p < (1.0 / 2.75)) ? (7.5625 * Math.Pow(p, 2.0)) : ((p < (2.0 / 2.75)) ? (((7.5625 * (p - (1.5 / 2.75))) * (p - (1.5 / 2.75))) + 0.75) : ((p < (2.5 / 2.75)) ? (((7.5625 * (p - (2.25 / 2.75))) * (p - (2.25 / 2.75))) + 0.9375) : (((7.5625 * (p - (2.625 / 2.75))) * (p - (2.625 / 2.75))) + 0.984375))));
                default: return p;
            }
        }

        double easing_in_out(int press, double p) {
            switch (press) {
                case 0: return p;
                case 1: return ((-(Math.Cos((Math.PI * p)) - 1.0)) / 2.0);
                case 2: return ((p < 0.5) ? (2.0 * Math.Pow(p, 2.0)) : (1.0 - (Math.Pow((((-2.0) * p) + 2.0), 2.0) / 2.0)));
                case 3: return ((p < 0.5) ? (4.0 * Math.Pow(p, 3.0)) : (1.0 - (Math.Pow((((-2.0) * p) + 2.0), 3.0) / 2.0)));
                case 4: return ((p < 0.5) ? (8.0 * Math.Pow(p, 4.0)) : (1.0 - (Math.Pow((((-2.0) * p) + 2.0), 4.0) / 2.0)));
                case 5: return ((p < 0.5) ? (16.0 * Math.Pow(p, 5.0)) : (1.0 - (Math.Pow((((-2.0) * p) + 2.0), 5.0) / 2.0)));
                case 6: return ((p == 0.0) ? 0.0 : ((p == 1.0) ? 1.0 : (((p < 0.5) ? Math.Pow(2.0, ((20.0 * p) - 10.0)) : (2.0 - Math.Pow(2.0, (((-20.0) * p) + 10.0)))) / 2.0)));
                case 7: return ((p < 0.5) ? ((1.0 - Math.Pow((1.0 - Math.Pow((2.0 * p), 2.0)), 0.5)) / 2.0) : ((Math.Pow((1.0 - Math.Pow((((-2.0) * p) + 2.0), 2.0)), 0.5) + 1.0) / 2.0));
                case 8: return ((p < 0.5) ? ((Math.Pow((2.0 * p), 2.0) * ((((2.5949095 + 1.0) * 2.0) * p) - 2.5949095)) / 2.0) : (((Math.Pow(((2.0 * p) - 2.0), 2.0) * (((2.5949095 + 1.0) * ((p * 2.0) - 2.0)) + 2.5949095)) + 2.0) / 2.0));
                case 9: return ((p == 0.0) ? 0.0 : ((p == 0.0) ? 1.0 : ((p < 0.5) ? (((-Math.Pow(2.0, ((20.0 * p) - 10.0))) * Math.Sin((((20.0 * p) - 11.125) * ((2.0 * Math.PI) / 4.5)))) / 2.0) : (((Math.Pow(2.0, (((-20.0) * p) + 10.0)) * Math.Sin((((20.0 * p) - 11.125) * ((2.0 * Math.PI) / 4.5)))) / 2.0) + 1.0))));
                case 10: return ((p < 0.5) ? ((1.0 - (((1.0 - (2.0 * p)) < (1.0 / 2.75)) ? (7.5625 * Math.Pow((1.0 - (2.0 * p)), 2.0)) : (((1.0 - (2.0 * p)) < (2.0 / 2.75)) ? (((7.5625 * ((1.0 - (2.0 * p)) - (1.5 / 2.75))) * ((1.0 - (2.0 * p)) - (1.5 / 2.75))) + 0.75) : (((1.0 - (2.0 * p)) < (2.5 / 2.75)) ? (((7.5625 * ((1.0 - (2.0 * p)) - (2.25 / 2.75))) * ((1.0 - (2.0 * p)) - (2.25 / 2.75))) + 0.9375) : (((7.5625 * ((1.0 - (2.0 * p)) - (2.625 / 2.75))) * ((1.0 - (2.0 * p)) - (2.625 / 2.75))) + 0.984375))))) / 2.0) : ((1.0 + ((((2.0 * p) - 1.0) < (1.0 / 2.75)) ? (7.5625 * Math.Pow(((2.0 * p) - 1.0), 2.0)) : ((((2.0 * p) - 1.0) < (2.0 / 2.75)) ? (((7.5625 * (((2.0 * p) - 1.0) - (1.5 / 2.75))) * (((2.0 * p) - 1.0) - (1.5 / 2.75))) + 0.75) : ((((2.0 * p) - 1.0) < (2.5 / 2.75)) ? (((7.5625 * (((2.0 * p) - 1.0) - (2.25 / 2.75))) * (((2.0 * p) - 1.0) - (2.25 / 2.75))) + 0.9375) : (((7.5625 * (((2.0 * p) - 1.0) - (2.625 / 2.75))) * (((2.0 * p) - 1.0) - (2.625 / 2.75))) + 0.984375))))) / 2.0));
                default: return p;
            }
        }

        double easing(double p) {
            if (!isValueExp) {
                switch (type) {
                    case 0: return easing_in(press, p);
                    case 1: return easing_out(press, p);
                    case 2: return easing_in_out(press, p);
                    default: return p;
                }
            }
            else {
                // return e->customEaseExpression(p);
                return 0.0;
            }
        }

        public double Interplate(double start, double end, double p, bool isColor) {
            if (!isColor) return start + ((end - start) * easing(p));
            else {
                p = easing(p);
                var sColor = ColorUtils.ToRGBA((uint)start);
                var eColor = ColorUtils.ToRGBA((uint)end);

                var r = sColor[0] + ((eColor[0] - sColor[0]) * p);
                var g = sColor[1] + ((eColor[1] - sColor[1]) * p);
                var b = sColor[2] + ((eColor[2] - sColor[2]) * p);
                var a = sColor[3] + ((eColor[3] - sColor[3]) * p);
                return (double)ColorUtils.ToUint(new double[] { r, g, b, a });
            }
        }
    }

    [Serializable]
    public class MilStoryBoard {
        public int index;
        public int type;
        public int layer;
        public string data;
        public List<MilAnimation> animations;

        public MilAnimationCollection animationCollection;
        public GameObject sbPrefab;
        public byte[] sbTextureBytes;
        public Texture2D sbTexture;
    }

    public class MilAnimationCollection {
        public int collectionType;
        public List<MilAnimation>[] animations = new List<MilAnimation>[Enum.GetNames(typeof(MilAnimationType)).Length];
        public int[] indexs = new int[Enum.GetNames(typeof(MilAnimationType)).Length];
        public double[] values = new double[Enum.GetNames(typeof(MilAnimationType)).Length];
        public double[] defaults = new double[Enum.GetNames(typeof(MilAnimationType)).Length];
        private double last_time;

        public void Init() {
            last_time = -1e9;
            for (int i = 0; i < animations.Length; i++) {
                animations[i] = new List<MilAnimation>();
                indexs[i] = 0;

                if (collectionType == (int)MilAnimationCollectionType.Line) {
                    values[i] = MilAnimationDefaults.Line[i];
                } else if (collectionType == (int)MilAnimationCollectionType.Note) {
                    values[i] = MilAnimationDefaults.Note[i];
                } else if (collectionType == (int)MilAnimationCollectionType.StoryBoard) {
                    values[i] = MilAnimationDefaults.StoryBoard[i];
                }

                defaults[i] = values[i];
            }
        }

        void InitSpeedEvents() {
            var speedEvents = animations[(int)MilAnimationType.Speed];
            var fp = 0.0;
            MilAnimation last = null;

            foreach (var e in speedEvents) {
                if (last == null) fp += e.timeSec * e.start;
                else fp += (e.timeSec - last.endTimeSec) * last.end;

                e.floorPosition = fp;
                fp += (e.endTimeSec - e.timeSec) * (e.start + e.end) / 2;
                last = e;
            }
        }

        public void AfterLoad() {
            for (int type = 0; type < animations.Length; type++) {
                var anims = animations[type];
                if (anims.Count > 1) {
                    anims.Sort((a, b) => a.timeSec.CompareTo(b.timeSec));
                }
            }

            InitSpeedEvents();
        }

        public void Update(double t, int only = -1) {
            if (t < last_time || true) {
                for (int type = 0; type < animations.Length; type++) {
                    indexs[type] = 0;
                }
            }
            last_time = t;

            for (int type = 0; type < animations.Length; type++) {
                var anims = animations[type];
                if (anims.Count == 0 || (only != -1 && only != type)) {
                    if (type == (int)MilAnimationType.Speed && (only == -1 || only == type)) {
                        values[type] = t * defaults[type];
                    }
                    continue;
                }
                
                while (indexs[type] < anims.Count - 1 && anims[indexs[type] + 1].timeSec <= t) {
                    indexs[type]++;
                }

                var e = anims[indexs[type]];
                values[type] = Interplate(e, t);

                if (type == (int)MilAnimationType.Speed) {
                    if (t < e.timeSec) values[type] = t * e.start;
                    else if (e.timeSec <= t && t <= e.endTimeSec) values[type] = e.floorPosition + (t - e.timeSec) * (values[type] + e.start) / 2;
                    else values[type] = e.floorPosition + (e.endTimeSec - e.timeSec) * (e.start + e.end) / 2 + (t - e.endTimeSec) * e.end;
                }
            }
        }

        public double Interplate(MilAnimation anim, double t) {
            var p = anim.timeSec == anim.endTimeSec ? 1f : (t - anim.timeSec) / (anim.endTimeSec - anim.timeSec);
            p = p < 0f ? 0f : (p > 1f ? 1f : p);
            var res = anim.ease.Interplate(anim.start, anim.end, p, anim.type == (int)MilAnimationType.Color);
            return res;
        }

        public double GetValue(int type) {
            return values[type];
        }

        public bool HasEvents(int type) {
            return animations[type].Count > 0;
        }
    }

    public enum MilAnimationCollectionType {
        Line = 0,
        Note = 1,
        StoryBoard = 2,
    }

    public static class MilAnimationDefaults {
        public static double[] Line = new double[] {
            0.0, -350.0, // position
            1.0, // transparency
            1.0, // size
            90.0, // rotation
            1.0, // flow speed
            0.0, 0.0, // relative position
            1.0, 1.0, // line body transparency
            0.0, 0.0, // ...
            1.0, // speed
            1.0, // whole transparency
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, // ...
            (double)0xffffffff, // color
            1e9, // visible area
        };
        
        public static double[] Note = new double[] {
            0.0, 0.0, // position
            1.0, // transparency
            1.0, // size
            0.0, // rotation
            1.0, // flow speed
            0.0, 0.0, // relative position
            1.0, 1.0, // ...
            0.0, 0.0, // ...
            1.0, // speed
            1.0, // ...
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, // ...
            (double)0xffffffff, // color
            0.0, // ...
        };
        
        public static double[] StoryBoard = new double[] {
            0.0, 0.0, // position
            1.0, // transparency
            1.0, // size
            0.0, // rotation
            1.0, // ...
            0.0, 0.0, // relative position
            1.0, 1.0, // ...
            1.0, 1.0, // storyboard size
            1.0, // ...
            1.0, // ...
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, // storyboard lb, rb, lt, rt
            (double)0xffffffff, // color
            0.0, // ...
        };
    }

    public enum MilAnimationType {
        PositionX = 0,
        PositionY = 1,
        Transparency = 2,
        Size = 3,
        Rotation = 4,
        FlowSpeed = 5,
        RelativeX = 6,
        RelativeY = 7,
        LineBodyTransparency = 8,
        LineHeadTransparency = 9,
        StoryBoardWidth = 10,
        StoryBoardHeight = 11,
        Speed = 12,
        WholeTransparency = 13,
        StoryBoardLeftBottomX = 14,
        StoryBoardLeftBottomY = 15,
        StoryBoardRightBottomX = 16,
        StoryBoardRightBottomY = 17,
        StoryBoardLeftTopX = 18,
        StoryBoardLeftTopY = 19,
        StoryBoardRightTopX = 20,
        StoryBoardRightTopY = 21,
        Color = 22,
        VisibleArea = 23,
    }

    public enum MilAnimationBearerType {
        Line = 0,
        Note = 1,
        StoryBoard = 2,
    }

    public enum MilStoryBoardType {
        Picture = 0,
        Text = 1,
        Mesh = 2,
    }

    public enum MilNoteType {
        Hit = 0,
        Drag = 1,
    }
}
