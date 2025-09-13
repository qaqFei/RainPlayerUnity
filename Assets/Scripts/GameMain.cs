using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System;
using System.Linq;
using System.Collections.Generic;

using Utils;
using Sasa;
using MilDataStructs;
using MilPlayment;

public class GameMain : MonoBehaviour
{
    public MilChart chart;
    public IntPtr sasaManager;
    public IntPtr sasaAudioClip;
    public IntPtr sasaHitClip;
    public IntPtr sasaDragClip;
    private IntPtr sasaHitSfx;
    private IntPtr sasaDragSfx;

    public StartPlay hub;
    public Canvas gameCanvas;
    public GameObject chartObjects;
    public GameObject sbLayer0;
    public GameObject sbLayer1;
    public GameObject sbLayer2;
    public GameObject linePrefab;
    public GameObject notePrefab;
    public GameObject holdNotePrefab;
    public GameObject sbPicturePrefab;
    public GameObject sbTextPrefab;
    public GameObject sbMeshPrefab;
    public GameObject hitParticlePrefab;
    public GameObject holdHitParticlePrefab;
    public GameObject goodHitParticlePrefab;
    public GameObject goodHoldHitParticlePrefab;
    public GameObject notePolyDisplayPrefab;
    public GameObject hitParticlesContainer;
    public GameObject hitCircPrefab;
    public GameObject hitCircsContainer;
    public GameObject GameUI;
    public GameObject PauseUI;
    public GameObject ResultUIRaw;
    public GameObject IlluImageGameObject;
    public GameObject IlluDimMaskGameObject;
    public RawImage chartDimMask;
    public Camera mainCamera;
    private bool isPlaying = false;
    private bool musicPaused = false;
    private bool userPaused = false;
    private IntPtr sasaMusic;
    private HashSet<HitCircInsWapper> hitCircInstances;
    private Keyboard keyboard;
    private Mouse mouse;
    private Touchscreen touchscreen;
    private double lastupdate_playment_time;
    private double t;
    private GameObject ResultUI;

    public Texture2D ntexTap;
    public Texture2D ntexExTap;
    public Texture2D ntexTapDouble;
    public Texture2D ntexExTapDouble;
    public Texture2D ntexDrag;
    public Texture2D ntexDragDouble;
    public Texture2D[] ntexHold;
    public Texture2D[] ntexExHold;
    public Texture2D[] ntexHoldDouble;
    public Texture2D[] ntexExHoldDouble;

    public Texture2D[] AllWeatherIcons;

    private const double MILWIDTH = 1920.0;
    private const double MILHEIGHT = 1080.0;
    private const double SPEED_UNIT = 120.0;
    private const double LINE_HEAD_SIZE = 0.0223;
    private const double NOTE_SIZE = LINE_HEAD_SIZE;
    private const double NOTE_SCALE = 335.0 / 185;
    private const double HOLD_ALTAS = 190;
    private const double HOLD_DISAPPEAR_TIME = 0.2;
    private const double HIT_CIRC_DUR = 0.5;
    private const double PLATMENT_UPDATE_FPS = 120.0;
    public double FLOW_SPEED = 1.66;
    public bool AUTOPLAY = false;
    public double OFFSET = 0.0;
    public double SPEED = 1.0;
    public bool ISDEBUG = false;

    private Vector2 canvasSize;

    private class HitCircInsWapper
    {
        public GameObject ins;
        public MilNote noteMaster;
        public MilJudgeLine lineMaster;
        public double st;
        public double seed;

        public HitCircInsWapper() {
            seed = UnityEngine.Random.Range(0f, 1f);
        }
    }
    
    void Start() {
        UpdateInputSystem();
        ResultUIRaw.SetActive(false);
    }

    void Awake() {
        UpdateInputSystem();
    }

    private void UpdateInputSystem() {
        keyboard = Keyboard.current;
        mouse = Mouse.current;
        touchscreen = Touchscreen.current;
    }

    private void OnApplicationQuit()
    {
        OnDestroy();
    }

    private void OnDestroy()
    {
        ReleaseSasa();
    }

    void UpdateCanvasSize() {
        canvasSize = gameCanvas.GetComponent<RectTransform>().sizeDelta;
    }

    Vector2 MilToCanvas(double x, double y) {
        return new Vector2((float)(x / MILWIDTH * canvasSize.x), (float)(y / MILHEIGHT * canvasSize.y));
    }

    double MilToCanvasX(double x) {
        return (x / MILWIDTH * canvasSize.x);
    }

    double MilToCanvasY(double y) {
        return (y / MILHEIGHT * canvasSize.y);
    }

    Vector2 LinePrefabRPToWorld(Vector2 rp) {
        return linePrefab.transform.TransformPoint(rp);
    }

    Vector2 LinePrefabRPToCanvas(Vector2 rp) {
        var canvasRectTransform = gameCanvas.GetComponent<RectTransform>();
        return RectTransformUtility.WorldToScreenPoint(mainCamera, LinePrefabRPToWorld(rp));
    }

    void Update() {
        UpdateCanvasSize();
        UpdateInputSystem();

        if (!isPlaying) return;
        libSasa.recover_if_needed(sasaManager);
        var music_length = libSasa.get_audio_clip_duration(sasaAudioClip);
        var music_position = libSasa.get_music_position(sasaMusic);
        t = music_position - OFFSET / 1000;

        if (keyboard != null) {
            for (Key key = Key.A; key <= Key.Z; key++) {
                if (keyboard[key].wasPressedThisFrame) chart.playment.touchstart(t, (int)key, 0, 0, true);
                if (keyboard[key].wasReleasedThisFrame) chart.playment.touchend(t, (int)key, 0, 0);
            }
        }

        if (mouse != null) {
            var position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame) chart.playment.touchstart(t, 1000, (int)position.x, (int)position.y, false);
            if (mouse.leftButton.isPressed && mouse.delta.ReadValue().magnitude > 0) chart.playment.touchmove(t, 1000, (int)position.x, (int)position.y);
            if (mouse.leftButton.wasReleasedThisFrame) chart.playment.touchend(t, 1000, (int)position.x, (int)position.y);
        }

        if (touchscreen != null) {
            var playment_sigs = chart.playment.get_all_sigs();
            var unity_sigs = new Dictionary<int, Vector2>();

            foreach (var touch in touchscreen.touches) {
                if (!touch.isInProgress) continue;
                var sig = 2000 + touch.touchId.ReadValue();
                unity_sigs[sig] = touch.position.ReadValue();
            }

            Debug.Log(unity_sigs);

            foreach (var sig in unity_sigs.Keys.Union(playment_sigs)) {
                if (sig < 2000) continue; // other input type
                if (!unity_sigs.ContainsKey(sig)) chart.playment.touchend(t, sig, 0, 0);
                else if (!playment_sigs.Contains(sig)) chart.playment.touchstart(t, sig, (int)unity_sigs[sig].x, (int)unity_sigs[sig].y, false);
                else chart.playment.touchmove(t, sig, (int)unity_sigs[sig].x, (int)unity_sigs[sig].y);
            }
        }

        while (lastupdate_playment_time + 1.0 / PLATMENT_UPDATE_FPS < t) {
            chart.playment.update(lastupdate_playment_time);
            lastupdate_playment_time += 1.0 / PLATMENT_UPDATE_FPS;
        }

        chart.Update(t);

        foreach (var line in chart.lines) {
            var linePos = MilToCanvas(
                line.animationCollection.GetValue((int)MilAnimationType.PositionX),
                line.animationCollection.GetValue((int)MilAnimationType.PositionY)
            );
            var lineTransparency = line.animationCollection.GetValue((int)MilAnimationType.Transparency);
            var lineSize = line.animationCollection.GetValue((int)MilAnimationType.Size) * MilConst.MilConst.NOTE_SIZE_SCALE;
            var lineRotation = line.animationCollection.GetValue((int)MilAnimationType.Rotation);
            var lineFlowSpeed = line.animationCollection.GetValue((int)MilAnimationType.FlowSpeed);
            var lineRelativePos = MilToCanvas(
                line.animationCollection.GetValue((int)MilAnimationType.RelativeX),
                line.animationCollection.GetValue((int)MilAnimationType.RelativeY)
            );
            var lineHeadTransparency = line.animationCollection.GetValue((int)MilAnimationType.LineHeadTransparency);
            var lineBodyTransparency = line.animationCollection.GetValue((int)MilAnimationType.LineBodyTransparency);
            var lineWholeTransparency = line.animationCollection.GetValue((int)MilAnimationType.WholeTransparency);
            var lineColor = ColorUtils.ToRGBA((uint)line.animationCollection.GetValue((int)MilAnimationType.Color));
            var lineVisArea = line.animationCollection.GetValue((int)MilAnimationType.VisibleArea);
            var lineFloorPosition = line.animationCollection.GetValue((int)MilAnimationType.Speed);
            var lineCenter = new Vector2(
                linePos.x + lineRelativePos.x,
                linePos.y + lineRelativePos.y
            );

            line.linePrefab.transform.localPosition = lineCenter;
            line.linePrefab.transform.localScale = new Vector3((float)lineSize, (float)lineSize, 1);
            line.lineHead.GetComponent<RawImage>().color = new Color((float)lineColor[0], (float)lineColor[1], (float)lineColor[2], (float)(lineHeadTransparency * lineTransparency * lineColor[3]));
            line.lineBody.GetComponent<RawImage>().color = new Color((float)lineColor[0], (float)lineColor[1], (float)lineColor[2], (float)(lineBodyTransparency * lineTransparency * lineColor[3]));
            line.linePrefab.transform.localRotation = Quaternion.Euler(0, 0, 90 - (float)lineRotation);
            line.currentCanvasPosition = lineCenter;

            foreach (var note in line.notes) {
                var noteClicked = (AUTOPLAY || note.isFake) ? note.timeSec <= t : note.head_judged && note.judge_time <= t;

                if (noteClicked && !note.clicked) {
                    note.clicked = true;

                    if (!note.isFake) {
                        if (note.type == (int)MilNoteType.Hit) libSasa.play_sfx(sasaHitSfx, (float)1.0);
                        else libSasa.play_sfx(sasaDragSfx, (float)1.0);
                    }
                }

                if (noteClicked && !note.hitParticlePlayed && note.timeSec + note.hitParticleOffset <= t && !note.isFake) {
                    note.hitParticlePlayed = true;
                    var pool = note.judge_isgood ? line.goodHitParticlePool : line.hitParticlePool;
                    var particle = pool.Get();
                    note.hitParticle = particle;
                    line.activeHitParticles.Add(particle);

                    var particleSys = particle.GetComponent<ParticleSystem>();
                    var autoReleaser = particle.GetComponent<AutoReleaseParticle>();
                    autoReleaser.pool = pool;
                    autoReleaser.activeHitParticles = line.activeHitParticles;
                    autoReleaser.master = note;
                    autoReleaser.isHold = false;
                    particleSys.Play();

                    if (note.isHold) {
                        pool = note.judge_isgood ? line.goodHoldHitParticlePool : line.holdHitParticlePool;
                        particle = pool.Get();
                        note.holdHitParticle = particle;
                        line.activeHitParticles.Add(particle);
                        particleSys = particle.GetComponent<ParticleSystem>();
                        particleSys.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        var particleSysMain = particleSys.main;
                        particleSysMain.duration = (float)((note.endTimeSec - t) / SPEED);
                        autoReleaser = particle.GetComponent<AutoReleaseParticle>();
                        autoReleaser.pool = pool;
                        autoReleaser.activeHitParticles = line.activeHitParticles;
                        autoReleaser.master = note;
                        autoReleaser.isHold = true;
                        particleSys.Play();
                    }

                    var hitCircIns = Instantiate(hitCircPrefab, hitCircsContainer.transform);
                    var insWapper = new HitCircInsWapper();
                    insWapper.ins = hitCircIns;
                    insWapper.noteMaster = note;
                    insWapper.lineMaster = line;
                    insWapper.st = t;
                    var insRawImg = hitCircIns.GetComponent<RawImage>();
                    insRawImg.material = new Material(insRawImg.material);
                    if (!note.judge_isgood) insRawImg.material.SetColor("_MultColor", new Color((float)0x96 / 0xff, (float)0x90 / 0xff, (float)0xfd / 0xff, 1));
                    else insRawImg.material.SetColor("_MultColor", new Color((float)133 / 0xff, (float)255 / 0xff, (float)189 / 0xff, 1));
                    insRawImg.material.SetFloat("_Seed", (float)insWapper.seed);
                    hitCircInstances.Add(insWapper);
                }

                var noteScale = note.animationCollection.GetValue((int)MilAnimationType.Size);
                var noteRotation = note.animationCollection.GetValue((int)MilAnimationType.Rotation);
                note.realScale = lineSize * noteScale;

                if (note.clicked && (!note.isHold || (note.isHold && note.endTimeSec + HOLD_DISAPPEAR_TIME < t))) {
                    if (note.notePrefab != null) {
                        if (note.isHold) line.holdNotePool.Release(note.notePrefab);
                        else line.notePool.Release(note.notePrefab);
                        note.notePrefab = null;

                        if (ISDEBUG && note.notePolyDisplayPrefab != null) {
                            foreach (var notePolyDisplay in note.notePolyDisplayPrefab) {
                                UnityEngine.Object.Destroy(notePolyDisplay);
                            }

                            note.notePolyDisplayPrefab = null;
                        }
                    }

                    continue;
                }

                var noteFpMult = MilToCanvasY(
                    SPEED_UNIT * FLOW_SPEED
                    * lineFlowSpeed * note.animationCollection.GetValue((int)MilAnimationType.FlowSpeed)
                    / MilConst.MilConst.NOTE_SIZE_SCALE
                );
                var noteFp = (note.floorPosition - lineFloorPosition) * noteFpMult;
                var noteWidth = (canvasSize.x + canvasSize.y) * NOTE_SIZE * NOTE_SCALE * noteScale;
                Texture2D noteTexture = null;
                Texture2D[] holdTextures = null;

                if (note.type == (int)MilNoteType.Hit && note.isHold && note.isMorebets && note.isAlwaysPerfect) holdTextures = ntexExHoldDouble;
                else if (note.type == (int)MilNoteType.Hit && note.isHold && note.isMorebets && !note.isAlwaysPerfect) holdTextures = ntexHoldDouble;
                else if (note.type == (int)MilNoteType.Hit && note.isHold && !note.isMorebets && note.isAlwaysPerfect) holdTextures = ntexExHold;
                else if (note.type == (int)MilNoteType.Hit && note.isHold && !note.isMorebets && !note.isAlwaysPerfect) holdTextures = ntexHold;
                else if (note.type == (int)MilNoteType.Hit && !note.isHold && note.isMorebets && note.isAlwaysPerfect) noteTexture = ntexExTapDouble;
                else if (note.type == (int)MilNoteType.Hit && !note.isHold && note.isMorebets && !note.isAlwaysPerfect) noteTexture = ntexTapDouble;
                else if (note.type == (int)MilNoteType.Hit && !note.isHold && !note.isMorebets && note.isAlwaysPerfect) noteTexture = ntexExTap;
                else if (note.type == (int)MilNoteType.Hit && !note.isHold && !note.isMorebets && !note.isAlwaysPerfect) noteTexture = ntexTap;
                else if (note.type == (int)MilNoteType.Drag && note.isMorebets) noteTexture = ntexDragDouble;
                else if (note.type == (int)MilNoteType.Drag && !note.isMorebets) noteTexture = ntexDrag;
                else continue; // ??

                var noteHeight = 0.0;
                var holdLength = 0.0;
                var noteCenter = note.GetPosition(t, noteFp, MilToCanvasX, MilToCanvasY);
                var noteTransform = note.GetCanvasTransform(lineCenter, lineRotation, lineSize, noteScale, noteRotation);
                Vector2[] notePoly = null;

                if (note.isHold) {
                    holdLength = Math.Max(0, (note.endFloorPosition - (note.timeSec <= t ? lineFloorPosition : note.floorPosition)) * noteFpMult);
                    noteHeight = holdLength + noteWidth;
                    notePoly = noteTransform.getCRectPoints(
                        (double)noteCenter.x,
                        (double)noteCenter.y + holdLength / 2,
                        noteWidth * note.realScale,
                        noteHeight * note.realScale / MilConst.MilConst.NOTE_SIZE_SCALE
                    );
                } else {
                    noteHeight = noteWidth / noteTexture.width * noteTexture.height;
                    notePoly = noteTransform.getCRectPoints(
                        (double)noteCenter.x,
                        (double)noteCenter.y,
                        noteWidth * note.realScale,
                        noteHeight * note.realScale
                    );
                }

                if (ISDEBUG) {
                    for (var i = 0; i < notePoly.Length; i++) {
                        notePoly[i] += canvasSize / 2;
                    }

                    if (note.notePolyDisplayPrefab == null) {
                        note.notePolyDisplayPrefab = new GameObject[] {
                            Instantiate(notePolyDisplayPrefab, gameCanvas.gameObject.transform),
                            Instantiate(notePolyDisplayPrefab, gameCanvas.gameObject.transform),
                            Instantiate(notePolyDisplayPrefab, gameCanvas.gameObject.transform),
                            Instantiate(notePolyDisplayPrefab, gameCanvas.gameObject.transform),
                        };
                    }

                    note.notePolyDisplayPrefab[0].transform.localPosition = notePoly[0] - canvasSize / 2;
                    note.notePolyDisplayPrefab[1].transform.localPosition = notePoly[1] - canvasSize / 2;
                    note.notePolyDisplayPrefab[2].transform.localPosition = notePoly[2] - canvasSize / 2;
                    note.notePolyDisplayPrefab[3].transform.localPosition = notePoly[3] - canvasSize / 2;
                }

                if (note.notePrefab != null || MathUtils.polygonInScreen(canvasSize.x, canvasSize.y, notePoly)) {
                    if (note.notePrefab == null) {
                        note.notePrefab = note.isHold ? line.holdNotePool.Get() : line.notePool.Get();
                    }

                    var noteTransparency = lineWholeTransparency * note.animationCollection.GetValue((int)MilAnimationType.Transparency);
                    var noteColorArr = ColorUtils.ToRGBA((uint)note.animationCollection.GetValue((int)MilAnimationType.Color));
                    var noteColor = new Color((float)noteColorArr[0], (float)noteColorArr[1], (float)noteColorArr[2], (float)(noteColorArr[3] * noteTransparency));

                    if (!AUTOPLAY && note.isHold && note.judge_ismiss && note.judge_misstime <= t && !note.isFake) {
                        var p = MathUtils.Fixorp((t - note.judge_misstime) / MilConst.MilConst.PLAY_NOTE_DISAPPEAR_TIME);
                        noteColor.r *= (float)(1.0 - 0.2 * p);
                        noteColor.g *= (float)(1.0 - 0.7 * p);
                        noteColor.b *= (float)(1.0 - 0.7 * p);
                    }

                    if (noteFp > MilToCanvasY(lineVisArea)) noteColor.a = (float)0.0;

                    note.notePrefab.transform.localPosition = noteCenter;
                    note.notePrefab.transform.localScale = new Vector3((float)noteScale, (float)noteScale, 1);
                    note.notePrefab.transform.localRotation = Quaternion.Euler(0, 0, (float)noteRotation);

                    if (!note.isHold) {
                        if (!AUTOPLAY) {
                            noteColor.a *= (float)(1.0 - (t - note.timeSec) / MilConst.MilConst.PLAY_NOTE_DISAPPEAR_TIME);
                        }

                        var noteTexGameObject = note.notePrefab.transform.Find("Texture").gameObject;
                        var noteImageComp = noteTexGameObject.GetComponent<RawImage>();
                        noteImageComp.texture = noteTexture;
                        noteImageComp.color = noteColor;
                        noteTexGameObject.GetComponent<AspectRatioFitter>().aspectRatio = (float)(noteWidth / noteHeight);
                    } else {
                        if (note.endTimeSec <= t) {
                            noteColor.a *= (float)(1.0 - (t - note.endTimeSec) / HOLD_DISAPPEAR_TIME);
                        }

                        var headGameObject = note.notePrefab.transform.Find("Head").gameObject;
                        var bodyGameObject = note.notePrefab.transform.Find("Body").gameObject;
                        var tailGameObject = note.notePrefab.transform.Find("Tail").gameObject;
                        var headImageComp = headGameObject.GetComponent<RawImage>();
                        var bodyImageComp = bodyGameObject.GetComponent<RawImage>();
                        var tailImageComp = tailGameObject.GetComponent<RawImage>();
                        headImageComp.texture = holdTextures[0];
                        bodyImageComp.texture = holdTextures[1];
                        tailImageComp.texture = holdTextures[2];
                        var bodyRectTransform = bodyGameObject.GetComponent<RectTransform>();
                        bodyRectTransform.sizeDelta = new Vector2(
                            (float)holdLength, bodyRectTransform.sizeDelta.y
                        );
                        var tailRectTransform = tailGameObject.GetComponent<RectTransform>();
                        tailGameObject.transform.localPosition = new Vector2(
                            0, (float)holdLength
                        );
                        headImageComp.color = bodyImageComp.color = tailImageComp.color = noteColor;
                    }
                }
            }

            foreach (var particle in line.activeHitParticles) {
                particle.transform.localPosition = new Vector3(lineCenter.x, lineCenter.y, -1);
                var autoReleaser = particle.GetComponent<AutoReleaseParticle>();
                particle.transform.localScale = new Vector3((float)autoReleaser.master.realScale, (float)autoReleaser.master.realScale, 1);
            }
        }

        var deletedCircIns = new List<HitCircInsWapper>();
        foreach (var insWapper in hitCircInstances) {
            if (t - insWapper.st > HIT_CIRC_DUR) {
                deletedCircIns.Add(insWapper);
                continue;
            }

            var rawImg = insWapper.ins.GetComponent<RawImage>();
            var material = rawImg.material;
            var p = (t - insWapper.st) / HIT_CIRC_DUR;
            var scale = (1.0 - Math.Pow(1.0 - p, 3.0)) * insWapper.noteMaster.realScale;
            material.SetFloat("_P", (float)p);
            insWapper.ins.transform.localPosition = insWapper.lineMaster.currentCanvasPosition;
            insWapper.ins.transform.localScale = new Vector3((float)scale, (float)scale, 1);
        }

        foreach (var insWapper in deletedCircIns) {
            hitCircInstances.Remove(insWapper);
            UnityEngine.Object.Destroy(insWapper.ins);
        }

        foreach (var sb in chart.storyboards) {
            if (sb.sbPrefab == null) continue;

            var sbPos = MilToCanvas(
                sb.animationCollection.GetValue((int)MilAnimationType.PositionX),
                sb.animationCollection.GetValue((int)MilAnimationType.PositionY)
            );
            var sbTransparency = sb.animationCollection.GetValue((int)MilAnimationType.Transparency);
            var sbSize = sb.animationCollection.GetValue((int)MilAnimationType.Size);
            var sbRotation = sb.animationCollection.GetValue((int)MilAnimationType.Rotation);
            var sbRelativePos = MilToCanvas(
                sb.animationCollection.GetValue((int)MilAnimationType.RelativeX),
                sb.animationCollection.GetValue((int)MilAnimationType.RelativeY)
            );
            var sbWidth = sb.animationCollection.GetValue((int)MilAnimationType.StoryBoardWidth);
            var sbHeight = sb.animationCollection.GetValue((int)MilAnimationType.StoryBoardHeight);
            var sbColorArr = ColorUtils.ToRGBA((uint)sb.animationCollection.GetValue((int)MilAnimationType.Color));
            var sbColor = new Color((float)sbColorArr[0], (float)sbColorArr[1], (float)sbColorArr[2], (float)(sbColorArr[3] * sbTransparency));
            var sbMergedPos = new Vector2(
                sbPos.x + sbRelativePos.x,
                sbPos.y + sbRelativePos.y
            );

            sb.sbPrefab.transform.localPosition = sbMergedPos;
            sb.sbPrefab.transform.localScale = new Vector3((float)(sbSize * sbWidth), (float)(sbSize * sbHeight), 1);
            sb.sbPrefab.transform.localRotation = Quaternion.Euler(0, 0, (float)sbRotation);
            
            if (sb.type == (int)MilStoryBoardType.Picture) {
                var sbGameObject = sb.sbPrefab.transform.Find("Texture");
                var sbImageComp = sbGameObject.GetComponent<RawImage>();
                sbImageComp.color = sbColor;
                if (sb.sbTexture == null) continue;
                var width = MilToCanvasX(sb.sbTexture.width);
                var height = width / sb.sbTexture.width * sb.sbTexture.height;
                sbGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2((float)width, (float)height);
            }
            else if (sb.type == (int)MilStoryBoardType.Text) {
                var sbGameObject = sb.sbPrefab.transform.Find("Text");
                var sbTextComp = sbGameObject.GetComponent<Text>();
                sbTextComp.color = sbColor;
            }
            else {}
        }

        var combo = AUTOPLAY ? chart.GetCombo(t) : chart.playment.combo;
        var score = AUTOPLAY ? (double)combo / chart.comboTimes.Count * 1010000 : chart.playment.score;
        var acc = AUTOPLAY ? 1.0 : chart.playment.acc;

        GameUI.transform.Find("Combo").gameObject.GetComponent<Text>().text = combo.ToString();
        GameUI.transform.Find("Score").gameObject.GetComponent<Text>().text = ((int)score).ToString("D7");
        GameUI.transform.Find("Acc").gameObject.GetComponent<Text>().text = $"{(acc * 100).ToString("F2")}%";
        GameUI.transform.Find("ComboText").gameObject.GetComponent<Text>().text = AUTOPLAY ? "AUTOPLAY" : "COMBO";

        var progress = music_position / music_length;
        GameUI.transform.Find("Progressbar").gameObject.GetComponent<RectTransform>().localScale = new Vector2((float)(progress * canvasSize.x), 1);
        var music_ended = music_position + 1e-2 >= music_length;

        if ((chart.comboTimes.Count != 0 && t > chart.comboTimes[chart.comboTimes.Count - 1] + 0.5) || music_ended) {
            isPlaying = false;
            if (!music_ended) {
                StartCoroutine(libSasa.fadeout_music(sasaMusic, 0.75));
            }

            BeforeEndplayAnimation();
            EndplayAnimation();
        }
    }

    void ReleaseSasa() {
        ReleaseSasaPlayIns();
        libSasa.destroy_clip(sasaAudioClip);
        sasaAudioClip = IntPtr.Zero;
        libSasa.destroy_clip(sasaHitClip);
        sasaHitClip = IntPtr.Zero;
        libSasa.destroy_clip(sasaDragClip);
        sasaDragClip = IntPtr.Zero;
    }

    void ReleaseSasaPlayIns() {
        libSasa.pause_music(sasaMusic);
        libSasa.destroy_music(sasaMusic);
        sasaMusic = IntPtr.Zero;
        libSasa.destroy_sfx(sasaHitSfx);
        sasaHitSfx = IntPtr.Zero;
        libSasa.destroy_sfx(sasaDragSfx);
        sasaDragSfx = IntPtr.Zero;
    }

    public void IntoPlay(bool isRetry = false) {
        gameCanvas.gameObject.SetActive(true);
        UpdateCanvasSize();

        chart.Init();
        chart.playment = new MilPlayment.MilPlayment(chart, Screen.width, Screen.height, AUTOPLAY);
        lastupdate_playment_time = -1.0 / PLATMENT_UPDATE_FPS;

        GameUI.transform.Find("Title").gameObject.GetComponent<TextOverflowEllipsis>().SetOverflowEllipsisText(chart.meta.name);
        GameUI.transform.Find("Difficulty").gameObject.GetComponent<Text>().text = $"{chart.meta.difficulty_name} {(int)chart.meta.difficulty}{(chart.meta.difficulty - (int)chart.meta.difficulty > 1e-6 ? "+" : "")}";

        var dimColor = chartDimMask.color;
        dimColor.a = (float)chart.meta.background_dim;
        chartDimMask.color = dimColor;

        foreach (var line in chart.lines) {
            line.linePrefab = Instantiate(linePrefab, chartObjects.transform);
            line.lineHead = line.linePrefab.transform.Find("LineHead").gameObject;
            line.lineBody = line.linePrefab.transform.Find("LineBody").gameObject;
            line.notePool = ResUtils.CreateGameObjectPool(notePrefab, 20, line.linePrefab);
            line.holdNotePool = ResUtils.CreateGameObjectPool(holdNotePrefab, 20, line.linePrefab);
            line.hitParticlePool = ResUtils.CreateGameObjectPool(hitParticlePrefab, 20, hitParticlesContainer);
            line.holdHitParticlePool = ResUtils.CreateGameObjectPool(holdHitParticlePrefab, 20, hitParticlesContainer);
            line.goodHitParticlePool = ResUtils.CreateGameObjectPool(goodHitParticlePrefab, 20, hitParticlesContainer);
            line.goodHoldHitParticlePool = ResUtils.CreateGameObjectPool(goodHoldHitParticlePrefab, 20, hitParticlesContainer);
            line.activeHitParticles = new HashSet<GameObject>();
        }

        foreach (var sb in chart.storyboards) {
            GameObject sbPrefab = null;
            GameObject sbLayer = null;

            if (sb.type == (int)MilStoryBoardType.Picture) sbPrefab = sbPicturePrefab;
            else if (sb.type == (int)MilStoryBoardType.Text) sbPrefab = sbTextPrefab;
            else if (sb.type == (int)MilStoryBoardType.Mesh) sbPrefab = sbMeshPrefab;
            else continue; // ???

            if (sb.layer == 0) sbLayer = sbLayer0;
            else if (sb.layer == 1) sbLayer = sbLayer1;
            else if (sb.layer == 2) sbLayer = sbLayer2;
            else continue; // ???

            sb.sbPrefab = Instantiate(sbPrefab, sbLayer.transform);

            if (sb.type == (int)MilStoryBoardType.Picture) {
                if (sb.sbTexture == null) continue;
                var sbImage = sb.sbPrefab.transform.Find("Texture").GetComponent<RawImage>();
                sbImage.texture = sb.sbTexture;
            } else if (sb.type == (int)MilStoryBoardType.Text) {
                var sbText = sb.sbPrefab.transform.Find("Text").GetComponent<Text>();
                sbText.text = sb.data;
            }
        }

        hitCircInstances = new HashSet<HitCircInsWapper>();
        PauseUI.SetActive(false);
        GameUI.transform.Find("Pause").gameObject.GetComponent<PauseButton>().Callback = new Action(() => {
            userPause();
        });
        
        if (!isRetry || true) loadSasaPlayIns(); // seek 之后 position 不会立刻变为 0, 所以这里重新加载
        libSasa.seek_music(sasaMusic, 0.0);
        libSasa.play_music(sasaMusic, (float)1.0);
        isPlaying = true;
    }

    void loadSasaPlayIns() {
        if (sasaManager == null || sasaManager == IntPtr.Zero) throw new Exception("sasaManager is null");
        if (sasaAudioClip == null || sasaAudioClip == IntPtr.Zero) throw new Exception("sasaAudioClip is null");
        if (sasaHitClip == null || sasaHitClip == IntPtr.Zero) throw new Exception("sasaHitClip is null");
        if (sasaDragClip == null || sasaDragClip == IntPtr.Zero) throw new Exception("sasaDragClip is null");

        sasaMusic = libSasa.create_music(sasaManager, sasaAudioClip, SPEED);
        sasaHitSfx = libSasa.create_sfx(sasaManager, sasaHitClip);
        sasaDragSfx = libSasa.create_sfx(sasaManager, sasaDragClip);
    }

    void OnApplicationPause(bool hasFocus) {
        if (isPlaying && !musicPaused && !userPaused) {
            libSasa.pause_music(sasaMusic);
            musicPaused = true;
        }
    }

    void OnApplicationFocus(bool pauseStatus) {
        if (isPlaying && musicPaused && !userPaused) {
            libSasa.play_music(sasaMusic, (float)1.0);
            musicPaused = false;
        }
    }

    private void userPause() {
        userPaused = true;
        musicPaused = true;
        libSasa.pause_music(sasaMusic);
    }

    void ReleasePrefabs() {
        foreach (var line in chart.lines) {
            ResUtils.DestroyGameObjectPool(line.notePool);
            ResUtils.DestroyGameObjectPool(line.holdNotePool);
            ResUtils.DestroyGameObjectPool(line.hitParticlePool);
            ResUtils.DestroyGameObjectPool(line.holdHitParticlePool);
            ResUtils.DestroyGameObjectPool(line.goodHitParticlePool);
            ResUtils.DestroyGameObjectPool(line.goodHoldHitParticlePool);
            Destroy(line.linePrefab);
            line.linePrefab = null;

            foreach (var note in line.notes) {
                if (note.notePolyDisplayPrefab != null) {
                    foreach (var i in note.notePolyDisplayPrefab) Destroy(i);
                    note.notePolyDisplayPrefab = null;
                }
            }
        }

        foreach (var sb in chart.storyboards) {
            if (sb.sbPrefab != null) Destroy(sb.sbPrefab);
            sb.sbPrefab = null;
        }

        foreach (var insW in hitCircInstances) {
            Destroy(insW.ins);
            insW.ins = null;
        }
        hitCircInstances.Clear();
    }

    public void BackToHub() {
        isPlaying = false;
        ReleasePrefabs();
        DestoryResultUI();
        gameCanvas.gameObject.SetActive(false);
        OnDestroy();
        hub.OnDestroy();
        hub.Start();
    }

    public void Retry() {
        isPlaying = false;
        ReleasePrefabs();
        DestoryResultUI();
        IntoPlay(true);
    }

    private void DestoryResultUI() {
        if (ResultUI != null) Destroy(ResultUI);
        ResultUI = null;
    }

    public void Continue() {
        StartCoroutine(InnerContinue());
    }

    private System.Collections.IEnumerator InnerContinue() {
        var btnContainer = PauseUI.transform.Find("ButtonContainer").gameObject;
        btnContainer.SetActive(false);

        var cutdown = PauseUI.transform.Find("Countdown").gameObject;
        cutdown.SetActive(true);

        var cutdown_text = cutdown.GetComponent<Text>();

        var cutdown_st = (double)Time.time;

        while (true) {
            var t = (double)Time.time - cutdown_st;
            var p = t % 1.0;
            p = 1.0 - Math.Pow(p, 2) * 0.4;

            cutdown_text.text = ((int)(3.0 - t) + 1).ToString();
            cutdown.transform.localScale = new Vector3((float)p, (float)p, 1);
            cutdown_text.color = new Color(1, 1, 1, (float)p);

            yield return null;
            if (Time.time - cutdown_st > 3.0) break;
        }

        PauseUI.SetActive(false);
        cutdown.SetActive(false);
        btnContainer.SetActive(true);
        userPaused = false;
        musicPaused = false;
        libSasa.play_music(sasaMusic, (float)1.0);
    }

    private void BeforeEndplayAnimation() {
        Debug.Log("EndplayAnimation");

        ResultUI = Instantiate(ResultUIRaw, gameCanvas.transform);
        ResultUI.SetActive(true);

        var IlluCloneContainer = ResultUI.transform.Find("IlluCloneContainer").gameObject;

        var cloneIlluImage = Instantiate(IlluImageGameObject, IlluCloneContainer.transform);
        cloneIlluImage.GetComponent<RawImage>().color = new Color(1, 1, 1, 0);
        cloneIlluImage.name = IlluImageGameObject.name;
        var cloneIlluDimMask = Instantiate(IlluDimMaskGameObject, IlluCloneContainer.transform);
        cloneIlluDimMask.GetComponent<RawImage>().color = new Color(1, 1, 1, 0);
        cloneIlluDimMask.name = IlluDimMaskGameObject.name;
    }

    public void runAnimation(double duration, Action<double, double> action, Action endAction = null) {
        action.Invoke(0, 0);
        StartCoroutine(_runAnimation(duration, action, endAction));
    }

    private System.Collections.IEnumerator _runAnimation(double duration, Action<double, double> action, Action endAction) {
        var start = Time.time;
        while (true) {
            var t = Math.Min(Time.time - start, duration);
            action.Invoke(t, t / duration);

            yield return null;
            if (t >= duration) break;
        }

        action.Invoke(duration, 1.0);
        endAction?.Invoke();
    }

    private void EndplayAnimation() {
        var IlluCloneContainer = ResultUI.transform.Find("IlluCloneContainer").gameObject;
        var ClonedIlluImage = IlluCloneContainer.transform.Find("IlluImage").gameObject;
        var ClonedIlluDimMask = IlluCloneContainer.transform.Find("IlluDimMask").gameObject;
        var LeftContainer = ResultUI.transform.Find("LeftContainer").gameObject;
        var ScoreText = LeftContainer.transform.Find("Score").gameObject;
        var AccText = LeftContainer.transform.Find("Acc").gameObject;
        var Icon = LeftContainer.transform.Find("Icon").gameObject;
        var DataItemContainer = LeftContainer.transform.Find("DataItemContainer").gameObject;
        var ScoreRawPos = ScoreText.GetComponent<RectTransform>().localPosition;
        var AccRawPos = AccText.GetComponent<RectTransform>().localPosition;
        Action<string, string> SetDataItem = (string name, string value) => {
            var dataItem = DataItemContainer.transform.Find(name).gameObject;
            dataItem.GetComponent<ResultDataItem>().rightText = value;
        };

        Icon.GetComponent<RawImage>().texture = AllWeatherIcons[UnityEngine.Random.Range(0, AllWeatherIcons.Length)];

        SetDataItem("Exact", (AUTOPLAY ? chart.comboTimes.Count : chart.playment.exact_cut).ToString());
        SetDataItem("Perfect", chart.playment.perfect_cut.ToString());
        SetDataItem("Great", chart.playment.great_cut.ToString());
        SetDataItem("Good", chart.playment.good_cut.ToString());
        SetDataItem("Bad", chart.playment.bad_cut.ToString());
        SetDataItem("Miss", chart.playment.miss_cut.ToString());

        var Score = chart.playment.score;
        var Acc = chart.playment.acc;

        runAnimation(1.0, (double t, double p) => {
            var IlluAlpha = 1.0 - Math.Pow(1.0 - p, 3);
            var IlluDimTarget = 0.9;
            var IlluDim = chart.meta.background_dim + (IlluDimTarget - chart.meta.background_dim) * IlluAlpha;
            var NumMultX = 1e10;
            var MulMult = NumMultX - (NumMultX - 1) * p;

            ClonedIlluImage.GetComponent<RawImage>().color = new Color(1, 1, 1, (float)IlluAlpha);
            ClonedIlluDimMask.GetComponent<RawImage>().color = new Color(1, 1, 1, (float)IlluDim);
            LeftContainer.GetComponent<RectTransform>().localPosition = new Vector3(0, (float)(-canvasSize.y * Math.Pow(1.0 - p, 4)), 0);
            ScoreText.GetComponent<Text>().text = (Score * MulMult).ToString("F0").PadLeft(7, '0'); // PadLeft 2nd arg must be char
            AccText.GetComponent<Text>().text = ((Acc * MulMult) * 100).ToString("F2") + "%";
        }, () => {
            runAnimation(0.5, (double t, double p) => {
                var ScoreAccDx = -canvasSize.x * 0.05 * (1.0 - Math.Pow(1.0 - p, 3));

                ScoreText.GetComponent<RectTransform>().localPosition = new Vector3((float)(ScoreRawPos.x + ScoreAccDx), ScoreRawPos.y, ScoreRawPos.z);
                AccText.GetComponent<RectTransform>().localPosition = new Vector3((float)(AccRawPos.x + ScoreAccDx), AccRawPos.y, AccRawPos.z);
                Icon.GetComponent<RawImage>().color = new Color(1, 1, 1, (float)(1.0 - Math.Pow(1.0 - p, 3)));
                Icon.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, (float)((1.0 - Math.Pow(1.0 - p, 3)) * 360));
            });
        });
    }
}
