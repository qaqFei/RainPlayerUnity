using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

using MilDataStructs;

namespace MilPlayment {
    public enum EnumJudgeState {
        Exact = 0,
        Perfect = 1,
        Great = 2,
        Good = 3,
        Bad = 4,
        Miss = 5
    }

    public static class JudgeRange {
        public const double Exact = 0.035;
        public const double Perfect = 0.07;
        public const double Great = 0.105;
        public const double Good = 0.14;
        public const double Bad = 0.155;
    }

    public class MilPlayment {
        public MilChart chart;
        public int w;
        public int h;
        public bool autoplay;
        public Vector2 judge_size;
        public double judge_dy;
        public List<MilPlayment.Touch> touches;
        public int frame;
        public double touch_hold_end;
        public int combo;
        public int max_combo;
        public int all_combo;
        public int exact_cut;
        public int perfect_cut;
        public int great_cut;
        public int good_cut;
        public int bad_cut;
        public int miss_cut;
        public Action<int, double> OnPlaymentNoteJudgeCallback;
        private double _acc;
        private double _fullacc;
        private double hold_lasts;

        public class Touch {
            public int sig;
            public int x;
            public int y;
            public double start_time;
            public bool is_key;
            public double hold_touch_end;

            public Touch(int sig, int x, int y, double start_time, bool is_key = false) {
                this.sig = sig;
                this.x = x;
                this.y = y;
                this.start_time = start_time;
                this.is_key = is_key;
                this.hold_touch_end = 0.0;
            }
        }

        public MilPlayment(MilChart chart, int w, int h, bool autoplay) {
            judge_size = new Vector2((float)(485.99991 / 1920 * w), (float)(2202.27645 / 1080 * h));
            judge_dy = 772.32375 / 1080 * h;
            this.chart = chart;
            touches = new List<Touch>();
            this.w = w;
            this.h = h;
            this.autoplay = autoplay;
            frame = 0;
            touch_hold_end = 0.0;
            combo = 0;
            max_combo = 0;
            all_combo = chart.comboTimes.Count;
            _acc = 0.0;
            _fullacc = 0.0;
            exact_cut = 0;
            perfect_cut = 0;
            great_cut = 0;
            good_cut = 0;
            bad_cut = 0;
            miss_cut = 0;
            hold_lasts = -1e9;
        }

        private void log(string msg) {
            // Debug.Log(msg);
        }

        private void _submit_accitem(double val) {
            _acc += val;
            _fullacc += 1.0;
        }

        public double acc {
            get {
                if (autoplay) return 1.0;
                return _fullacc != 0.0 ? (_acc / _fullacc) : 1.0;
            }
        }

        public double score {
            get {
                if (autoplay) return 1010000.0;
                return (_acc / all_combo) * 100_0000.0 + (double)max_combo / all_combo * 10000.0;
            }
        }

        private void _add_combo() {
            combo++;
            log($"combo update to {combo}");
            max_combo = Math.Max(max_combo, combo);
        }

        private void _remove_combo() {
            combo = 0;
            log("combo reset to 0");
        }

        private Touch _get_touch(int sig) {
            return touches.Find(t => t.sig == sig);
        }

        public int[] get_all_sigs() {
            return touches.Select(t => t.sig).ToArray();
        }

        private Vector2 NorMilPos(double x, double y) {
            return new Vector2((float)(x / 1920 * w), (float)(y / 1080 * h));
        }

        private double ToScreenX(double x) {
            return x / 1920 * w;
        }

        private double ToScreenY(double y) {
            return y / 1080 * h;
        }

        private bool _ishit(double t, MilNote note, Touch touch) {
            if (touch.is_key) return true;

            var line = note.master;
            note.animationCollection.Update(t);
            line.animationCollection.Update(t);

            var linePos = NorMilPos(
                line.animationCollection.GetValue((int)MilAnimationType.PositionX),
                line.animationCollection.GetValue((int)MilAnimationType.PositionY)
            );
            var lineRelativePos = NorMilPos(
                line.animationCollection.GetValue((int)MilAnimationType.RelativeX),
                line.animationCollection.GetValue((int)MilAnimationType.RelativeY)
            );
            var lineCenter = new Vector2(
                linePos.x + lineRelativePos.x,
                linePos.y + lineRelativePos.y
            );
            var lineSize = line.animationCollection.GetValue((int)MilAnimationType.Size);
            var lineRotation = line.animationCollection.GetValue((int)MilAnimationType.Rotation);
            var noteScale = note.animationCollection.GetValue((int)MilAnimationType.Size);
            var noteRotation = note.animationCollection.GetValue((int)MilAnimationType.Rotation);

            var notePos = note.GetPosition(t, 0.0, ToScreenX, ToScreenY);
            var transform = note.GetCanvasTransform(lineCenter, lineRotation, lineSize, noteScale, noteRotation);
            var touchPoint = transform.getInverse().getPoint((double)touch.x, (double)touch.y);

            return (
                -judge_size[0] / 2 <= touchPoint.x &&
                touchPoint.x <= judge_size[0] / 2 &&
                -judge_size[1] / 2 <= touchPoint.y - judge_dy &&
                touchPoint.y <= judge_size[1] / 2
            );
        }

        private int _get_judge_state(MilNote note, double offset) {
            offset = Math.Abs(offset);
            int res;

            if (offset <= JudgeRange.Exact) res = (int)EnumJudgeState.Exact;
            else if (offset <= JudgeRange.Perfect) res = (int)EnumJudgeState.Perfect;
            else if (offset <= JudgeRange.Great) res = (int)EnumJudgeState.Great;
            else if (offset <= JudgeRange.Good) res = (int)EnumJudgeState.Good;
            else if (offset <= JudgeRange.Bad) res = (int)EnumJudgeState.Bad;
            else res = (int)EnumJudgeState.Miss;

            if (note.isAlwaysPerfect && _judge_state_ishit(res)) res = (int)EnumJudgeState.Exact;

            return res;
        }

        private bool _judge_state_ishit(int state) {
            return (int)EnumJudgeState.Exact <= state && state <= (int)EnumJudgeState.Good;
        }

        private List<MilNote> _get_notes(double t) {
            var res = new List<MilNote>();
            foreach (var line in chart.lines) {
                foreach (var note in line.notes) {
                    if (
                        !note.isFake &&
                        (
                            ( !note.isHold &&
                            note.timeSec - JudgeRange.Bad * 2 <= t &&
                            t <= note.timeSec + JudgeRange.Bad * 2 ) ||
                            ( note.isHold &&
                            note.timeSec - JudgeRange.Bad * 2 <= t &&
                            t <= note.endTimeSec + JudgeRange.Bad * 2 )
                        )
                    ) {
                        res.Add(note);
                    }
                }
            }
            res.Sort((a, b) => Math.Abs(a.timeSec - t).CompareTo(Math.Abs(b.timeSec - t)));
            return res;
        }

        public void touchstart(double t, int sig, int x, int y, bool is_key = false) {
            if (autoplay) return;
            x -= w / 2; y = h / 2 - y;
            var touch = _get_touch(sig);

            if (touch == null) {
                touch = new Touch(sig, x, y, t, is_key);
                touches.Add(touch);
                log($"touch start: {touch.sig}, time: {t}, ({x}, {y})");
            } else {
                touch.is_key = is_key;
                touch.x = x;
                touch.y = y;
                touch.start_time = t;
                log($"touch start: {touch.sig}, time: {t}, ({x}, {y}) (was active)");
            }

            foreach (var note in _get_notes(t)) {
                var line = note.master;

                if (_ishit(t, note, touch) && !note.head_judged && note.timeSec - JudgeRange.Bad < t) {
                    if (note.type == (int)MilNoteType.Hit) {
                        var offset = t - note.timeSec;
                        note.judge_state = _get_judge_state(note, offset);

                        if (_judge_state_ishit(note.judge_state)) {
                            note.judge_time = t;
                            note.judge_hited = true;
                            note.head_judged = true;
                            note.hitParticleOffset = offset;

                            OnPlaymentNoteJudgeCallback?.Invoke(note.judge_state, offset);

                            if (note.isHold) {
                                note.judge_holdlastcheck = t;
                                touch_hold_end = Math.Max(note.endTimeSec, touch_hold_end);
                                touch.hold_touch_end = touch_hold_end;

                                foreach (var tc in touches) {
                                    if (Math.Abs(tc.start_time - hold_lasts) < 0.3) {
                                        tc.hold_touch_end = touch_hold_end;
                                    }
                                }
                            }

                            _add_combo();
                            log($"hit note: {note.index}, state: {note.judge_state}, offset: {offset}, ishold: {note.isHold}");

                            if (note.judge_state == (int)EnumJudgeState.Exact) {
                                _submit_accitem(1.0);
                                exact_cut++;
                            } else if (note.judge_state == (int)EnumJudgeState.Perfect) {
                                _submit_accitem(0.9);
                                perfect_cut++;
                            } else if (note.judge_state == (int)EnumJudgeState.Great) {
                                _submit_accitem(0.6);
                                great_cut++;
                                note.judge_isgood = true;
                            } else if (note.judge_state == (int)EnumJudgeState.Good) {
                                _submit_accitem(0.3);
                                good_cut++;
                                note.judge_isgood = true;
                            }

                            if (note.isHold) hold_lasts = note.timeSec;

                            break;
                        }

                        if (note.judge_state == (int)EnumJudgeState.Bad && !note.isHold) {
                            OnPlaymentNoteJudgeCallback?.Invoke(note.judge_state, offset);
                            note.judge_time = t;
                            note.judge_hited = true;
                            _submit_accitem(0.15);
                            bad_cut++;
                            _remove_combo();
                            log($"bad note: {note.index}, offset: {offset}");
                            break;
                        }

                        note.judge_state = (int)EnumJudgeState.Miss;
                    }
                }
            }

            update(t);
        }

        public void touchmove(double t, int sig, int x, int y) {
            if (autoplay) return;
            x -= w / 2; y = h / 2 - y;
            var touch = _get_touch(sig);

            if (touch != null) {
                touch.x = x;
                touch.y = y;
                log($"touch move: {touch.sig}, time: {t}, ({x}, {y})");
            } else {
                log($"touch move: {sig}, time: {t}, ({x}, {y}) (not found)");
            }

            update(t);
        }

        public void touchend(double t, int sig, int x, int y) {
            if (autoplay) return;
            x -= w / 2; y = h / 2 - y;
            var touch = _get_touch(sig);

            if (touch != null) {
                touches.Remove(touch);
                log($"touch end: {touch.sig}, time: {t}");
            } else {
                log($"touch end: {sig}, time: {t}, ({x}, {y}) (not found)");
            }

            update(t);
        }

        public void update(double t) {
            if (autoplay) return;
            frame++;

            string sigs = string.Join(", ", touches.Select(touch => touch.sig.ToString()));
            log($"Active touch sigs ({touches.Count}): {sigs}");

            foreach (var note in _get_notes(t)) {
                foreach (var touch in touches) {
                    if (note.type == (int)MilNoteType.Drag && !note.head_judged && _ishit(t, note, touch)) {
                        var offset = t - note.timeSec;
                        var state = _get_judge_state(note, offset);

                        if (_judge_state_ishit(state)) {
                            note.prejudge_touches.Add(touch);
                            if (!note.judge_drag_prejudge) {
                                note.judge_drag_prejudge = true;
                                note.prejudge_time = t;
                                log($"prejudge drag: {note.index}, state: {state}, offset: {offset}");
                            }
                        }
                    }

                    if (note.isHold && touch.hold_touch_end >= t && note.head_judged) {
                        note.judge_holdlastcheck = t;
                        log($"hold update: {note.index}, chart_time: {t}, frame: {frame}");
                    }
                }

                if (note.type == (int)MilNoteType.Drag && !note.head_judged && note.judge_drag_prejudge && note.timeSec <= t) {
                    note.judge_hited = true;
                    note.head_judged = true;
                    note.judge_time = Math.Max(note.timeSec, t);
                    note.judge_state = (int)EnumJudgeState.Exact;
                    note.hitParticleOffset = note.judge_time - note.timeSec;
                    OnPlaymentNoteJudgeCallback?.Invoke(note.judge_state, 0.0);
                    
                    foreach (var tc in note.prejudge_touches) {
                        tc.hold_touch_end = touch_hold_end;
                    }
                    note.prejudge_touches.Clear();

                    _submit_accitem(1.0);
                    exact_cut++;
                    _add_combo();
                    log($"judge drag: {note.index}, chart_time: {t}");
                }

                if (
                    note.isHold && (
                        (note.head_judged && !note.judge_ismiss && t - note.judge_holdlastcheck >= 0.05 && note.endTimeSec - 0.3 >= t) ||
                        (!note.head_judged && t - note.timeSec >= JudgeRange.Bad)
                    ) && !note.judge_ismiss
                ) {
                    note.judge_ismiss = true;
                    note.judge_misstime = t;
                    _remove_combo();
                    _submit_accitem(0.0);
                    miss_cut++;

                    if (!note.head_judged) {
                        OnPlaymentNoteJudgeCallback?.Invoke(note.judge_state, t - note.timeSec);
                    }

                    if (note.holdHitParticle != null) {
                        note.holdHitParticle.GetComponent<ParticleSystem>().Stop();
                    }

                    log($"hold miss: {note.index}, chart_time: {t}, frame: {frame}");
                }

                if (note.isHold && note.head_judged && !note.judge_ismiss && note.endTimeSec <= t && !note.judge_gived_endcombo) {
                    _submit_accitem(1.0);
                    _add_combo();
                    note.judge_gived_endcombo = true;

                    if (note.judge_state == (int)EnumJudgeState.Exact) exact_cut++;
                    else if (note.judge_state == (int)EnumJudgeState.Perfect) perfect_cut++;
                    else if (note.judge_state == (int)EnumJudgeState.Great) great_cut++;
                    else if (note.judge_state == (int)EnumJudgeState.Good) good_cut++;

                    log($"hold ended: {note.index}, chart_time: {t}, frame: {frame}");
                }

                if (!note.isHold && !note.head_judged && !note.judge_ismiss && t - note.timeSec >= JudgeRange.Bad) {
                    note.judge_ismiss = true;
                    OnPlaymentNoteJudgeCallback?.Invoke(note.judge_state, t - note.timeSec);
                    note.judge_misstime = t;
                    _submit_accitem(0.0);
                    miss_cut++;
                    _remove_combo();
                    log($"note miss: {note.index}, chart_time: {t}, frame: {frame}");
                }
            }
        }
    }
}
