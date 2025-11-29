using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

using Utils;
using Sasa;
using MilDataStructs;
using MilConst;

public class StartPlay : MonoBehaviour, I18nSupported
{
    public SelectChartButton chartSelector;
    public Text stateText;
    public Canvas hubCanvas;
    public Canvas gameCanvas;
    public GameMain gameMain;
    public IntPtr sasaManager;
    public GameObject flowSpeedSlider;
    public GameObject noteSizeSlider;
    public GameObject offsetSlider;
    public GameObject speedSlider;
    public GameObject musicVolSlider;
    public GameObject hitsoundVolSlider;
    public Toggle autoplayToggle;
    public Toggle debugToggle;
    public Toggle ChordHLToggle;
    public Toggle ELIndicatorToggle;
    public Toggle ShowTouchPointToggle;
    public Toggle OklchColorInterplateToggle;
    public InputField comboTextInputField;
    private Button buttonSelf;
    private Button selectChartButton;

    public RawImage chartImage;
    private byte[] chartImageBytes;

    private ChartMeta meta;
    private MilChart chart;
    private IntPtr sasaAudioClip;
    private string selectedPath;

    private Action stateSetter;

    public void Start() {
        if (stateText != null) stateText.text = "";
        if (chartSelector != null) selectChartButton = chartSelector.GetComponent<Button>();

        buttonSelf = GetComponent<Button>();
        hubCanvas.gameObject.SetActive(true);
        gameCanvas.gameObject.SetActive(false);
        sasaManager = libSasa.create_audio_manager();
        Application.targetFrameRate = 1440;

        #if UNITY_WEBGL && !UNITY_EDITOR
        updateWebGLParams();
        #endif
    }

    void Update() {
        
    }

    #if UNITY_WEBGL && !UNITY_EDITOR
    void updateWebGLParams() {
        var p_flowSpeed = WebGLHelper.WebGLHelper_GetUrlParamWarpper("flowSpeed");
        if (p_flowSpeed != null) flowSpeedSlider.GetComponent<Slider>().value = float.Parse(p_flowSpeed);

        var p_noteSize = WebGLHelper.WebGLHelper_GetUrlParamWarpper("noteSize");
        if (p_noteSize != null) noteSizeSlider.GetComponent<Slider>().value = float.Parse(p_noteSize);

        var p_offset = WebGLHelper.WebGLHelper_GetUrlParamWarpper("offset");
        if (p_offset != null) offsetSlider.GetComponent<Slider>().value = float.Parse(p_offset);

        var p_speed = WebGLHelper.WebGLHelper_GetUrlParamWarpper("speed");
        if (p_speed != null) speedSlider.GetComponent<Slider>().value = float.Parse(p_speed);

        var p_musicVol = WebGLHelper.WebGLHelper_GetUrlParamWarpper("musicVol");
        if (p_musicVol != null) musicVolSlider.GetComponent<Slider>().value = float.Parse(p_musicVol);

        var p_hitsoundVol = WebGLHelper.WebGLHelper_GetUrlParamWarpper("hitsoundVol");
        if (p_hitsoundVol != null) hitsoundVolSlider.GetComponent<Slider>().value = float.Parse(p_hitsoundVol);

        var p_autoPlay = WebGLHelper.WebGLHelper_GetUrlParamWarpper("autoPlay");
        if (p_autoPlay != null) autoplayToggle.isOn = bool.Parse(p_autoPlay);

        var p_debug = WebGLHelper.WebGLHelper_GetUrlParamWarpper("debug");
        if (p_debug != null) debugToggle.isOn = bool.Parse(p_debug);

        var p_chordHL = WebGLHelper.WebGLHelper_GetUrlParamWarpper("chordHL");
        if (p_chordHL != null) ChordHLToggle.isOn = bool.Parse(p_chordHL);

        var p_elIndicator = WebGLHelper.WebGLHelper_GetUrlParamWarpper("elIndicator");
        if (p_elIndicator != null) ELIndicatorToggle.isOn = bool.Parse(p_elIndicator);

        var p_showTouchPoint = WebGLHelper.WebGLHelper_GetUrlParamWarpper("showTouchPoint");
        if (p_showTouchPoint != null) ShowTouchPointToggle.isOn = bool.Parse(p_showTouchPoint);

        var p_oklchColorInterplate = WebGLHelper.WebGLHelper_GetUrlParamWarpper("oklchColorInterplate");
        if (p_oklchColorInterplate != null) OklchColorInterplateToggle.isOn = bool.Parse(p_oklchColorInterplate);

        var p_comboText = WebGLHelper.WebGLHelper_GetUrlParamWarpper("comboText");
        if (p_comboText != null) comboTextInputField.text = p_comboText;
    }
    #endif

    void setStateSetter(Action setter) {
        stateSetter = setter;
        stateSetter.Invoke();
    }

    public void OnI18nChanged() {
        stateSetter?.Invoke();
    }

    void disableButton() {
        if (buttonSelf != null) buttonSelf.interactable = false;
        if (selectChartButton != null) selectChartButton.interactable = false;
    }

    void enableButton() {
        if (buttonSelf != null) buttonSelf.interactable = true;
        if (selectChartButton != null) selectChartButton.interactable = true;
    }

    public void ButtonOnClick() {
        Debug.Log("Start play button clicked");
        if (chartSelector == null) return;

        selectedPath = chartSelector.selectedPath;
        if (selectedPath == null) {
            setStateSetter(() => {
                stateText.text = MilConst.MilConst.i18n.GetText("StartPlay-NoChartSelected");
            });
            return;
        };
        Debug.Log($"Selected path: {selectedPath}");

        if (!File.Exists(selectedPath)) {
            setStateSetter(() => {
                stateText.text = $"{MilConst.MilConst.i18n.GetText("StartPlay-FileNotFound")}: {selectedPath}";
            });
            return;
        }

        setStateSetter(() => {
            stateText.text = MilConst.MilConst.i18n.GetText("StartPlay-LoadingChart");
        });
        disableButton();

        ChartPreload(() => {
            StartCoroutine(ChartLoader());
        });
    }

    private void ChartPreload(Action callback) {
        StartCoroutine(ResUtils.ReadStreamingAsset("hit.ogg", (byte[] hit_bytes) => {
            StartCoroutine(ResUtils.ReadStreamingAsset("drag.ogg", (byte[] drag_bytes) => {
                gameMain.sasaHitClip = libSasa.load_audio_clip(ResUtils.CreateTempFile(hit_bytes));
                gameMain.sasaDragClip = libSasa.load_audio_clip(ResUtils.CreateTempFile(drag_bytes));
                callback.Invoke();
            }));
        }));
    }

    public System.Collections.IEnumerator StartPlayNextFrame() {
        yield return null;
        ButtonOnClick();
    }

    private System.Collections.IEnumerator ChartLoader() {
        #if UNITY_WEBGL && !UNITY_EDITOR
        WebGLHelper.WebGLHelper_ChartPlayerStartedLoad();
        #endif

        yield return null;

        Exception err = null;

        try {
            using (ZipArchive chartArchive = ZipFile.OpenRead(selectedPath)) {
                Debug.Log("Opened chart archive");
        
                var metaEntry = ZipUtils.GetEntry(chartArchive, "meta.json");
                if (metaEntry == null) throw new Exception("meta.json not found in chart archive");
        
                using (var metaStream = metaEntry.Open())
                using (var metaReader = new StreamReader(metaStream)) {
                    string metaJsonContent = metaReader.ReadToEnd();
                    meta = JsonUtility.FromJson<ChartMeta>(metaJsonContent);
                    Debug.Log($"Chart metadata: {metaJsonContent}");
                }
        
                var chartEntry = ZipUtils.GetEntry(chartArchive, meta.chart_file);
                var audioEntry = ZipUtils.GetEntry(chartArchive, meta.audio_file);
                var imageEntry = ZipUtils.GetEntry(chartArchive, meta.image_file);
        
                if (chartEntry == null) throw new Exception("Chart file not found in chart archive");
                if (audioEntry == null) throw new Exception("Audio file not found in chart archive");
                if (imageEntry == null) throw new Exception("Image file not found in chart archive");
        
                using (var chartStream = chartEntry.Open())
                using (var chartReader = new StreamReader(chartStream)) {
                    string chartJsonContent = chartReader.ReadToEnd();
                    chart = JsonUtility.FromJson<MilChart>(chartJsonContent);
                }
        
                using (var imageStream = imageEntry.Open()) {
                    chartImageBytes = ResUtils.ReadAllBytes(imageStream);
                    Debug.Log($"Loaded image data: {chartImageBytes.Length} bytes");
                }
        
                using (var audioStream = audioEntry.Open()) {
                    sasaAudioClip = libSasa.load_audio_clip(ResUtils.CreateTempFile(ResUtils.ReadAllBytes(audioStream)));
                    Debug.Log($"Loaded sasa audio clip: {sasaAudioClip}");
                }
        
                foreach (var sb in chart.storyboards) {
                    if (sb.type == (int)MilStoryBoardType.Picture) {
                        foreach (var ext in new string[] { "", ".jpg", ".png", ".jpeg" }) {
                            try {
                                using (var textureStream = ZipUtils.GetEntry(chartArchive, $"res/{sb.data}{ext}").Open()) {
                                    sb.sbTextureBytes = ResUtils.ReadAllBytes(textureStream);
                                }
                                break;
                            } catch (Exception e) {
                                // Debug.Log($"Loading storyboard texture {sb.data} extname {ext} failed: {e.Message}, trying next");
                            }
                        }
                    }
                }
            }
        } catch (Exception e) {
            Debug.Log($"Error when loading chart (async): {e.Message}");
            Debug.LogException(e);
            err = e;
            yield break;

            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLHelper.WebGLHelper_ChartPlayerLoadFailed();
            #endif
        }

        if (err == null) {
            try {
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(chartImageBytes);
                chartImage.texture = tex;

                AspectRatioFitter fitter = chartImage.GetComponent<AspectRatioFitter>();
                fitter.aspectRatio = (float)tex.width / tex.height;

                foreach (var sb in chart.storyboards) {
                    if (sb.type == (int)MilStoryBoardType.Picture) {
                        try {
                            Texture2D sbTex = new Texture2D(2, 2);
                            sbTex.LoadImage(sb.sbTextureBytes);
                            sb.sbTexture = sbTex;
                        }
                        catch (Exception e) {
                            Debug.LogWarning($"Error when loading storyboard texture: {e.Message}");
                        }
                    }
                }

                gameMain.chart = chart;
                gameMain.sasaManager = sasaManager;
                gameMain.sasaAudioClip = sasaAudioClip;
                gameMain.FLOW_SPEED = flowSpeedSlider.GetComponent<Slider>().value;
                MilConst.MilConst.NOTE_SIZE_SCALE = noteSizeSlider.GetComponent<Slider>().value;
                gameMain.AUTOPLAY = autoplayToggle.GetComponent<Toggle>().isOn;
                gameMain.OFFSET = offsetSlider.GetComponent<Slider>().value;
                gameMain.SPEED = speedSlider.GetComponent<Slider>().value;
                gameMain.ISDEBUG = debugToggle.GetComponent<Toggle>().isOn;
                gameMain.CHORDHL = ChordHLToggle.GetComponent<Toggle>().isOn;
                gameMain.ELINDICATOR = ELIndicatorToggle.GetComponent<Toggle>().isOn;
                gameMain.COMBOTEXT = comboTextInputField.GetComponent<InputField>().text;
                gameMain.SHOWTOUCHPOINT = ShowTouchPointToggle.GetComponent<Toggle>().isOn;
                gameMain.MUSICVOL = musicVolSlider.GetComponent<Slider>().value;
                gameMain.HITSOUNDVOL = hitsoundVolSlider.GetComponent<Slider>().value;
                MilConst.MilConst.EnableOklchInterplate = OklchColorInterplateToggle.GetComponent<Toggle>().isOn;
                hubCanvas.gameObject.SetActive(false);

                setStateSetter(() => {
                    stateText.text = MilConst.MilConst.i18n.GetText("StartPlay-ChartLoaded");
                });
                gameMain.IntoPlay();
            } catch (Exception e) {
                setStateSetter(() => {
                    stateText.text = $"{MilConst.MilConst.i18n.GetText("StartPlay-Error")}: {e.Message}";
                });
                Debug.Log($"Error when loading chart: {e.Message}");
                Debug.LogException(e);
                err = e;
                yield break;

                #if UNITY_WEBGL && !UNITY_EDITOR
                WebGLHelper.WebGLHelper_ChartPlayerLoadFailed();
                #endif
            }
        }

        enableButton();
        #if UNITY_WEBGL && !UNITY_EDITOR
        WebGLHelper.WebGLHelper_ChartPlayerLoaded();
        #endif

        yield break;
    }

    public void OnApplicationQuit()
    {
        OnDestroy();
    }

    public void OnDestroy()
    {
        ReleaseSasa();
    }

    void ReleaseSasa() {
        if (sasaManager != null && sasaManager != IntPtr.Zero) {
            libSasa.destroy_manager(sasaManager);
        }
    }
}
