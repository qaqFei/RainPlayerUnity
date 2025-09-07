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

public class StartPlay : MonoBehaviour
{
    public SelectChartButton chartSelector;
    public Text stateText;
    public Canvas hubCanvas;
    public Canvas gameCanvas;
    public GameMain gameMain;
    public IntPtr sasaManager;
    public GameObject flowSpeedSlider;
    public GameObject noteSizeSlider;
    public Toggle autoplayToggle;
    private Button buttonSelf;
    private Button selectChartButton;

    public RawImage chartImage;
    private byte[] chartImageBytes;

    private ChartMeta meta;
    private MilChart chart;
    private IntPtr sasaAudioClip;

    public void Start() {
        if (stateText != null) stateText.text = "";
        if (chartSelector != null) selectChartButton = chartSelector.GetComponent<Button>();

        buttonSelf = GetComponent<Button>();
        hubCanvas.gameObject.SetActive(true);
        gameCanvas.gameObject.SetActive(false);
        sasaManager = libSasa.create_audio_manager();
        Application.targetFrameRate = 1440;
    }

    void Update() {
        
    }

    void setState(string state) {
        if (stateText != null) stateText.text = state;
    }

    void disableButton() {
        if (buttonSelf != null) buttonSelf.interactable = false;
        if (selectChartButton != null) selectChartButton.interactable = false;
    }

    void enableButton() {
        if (buttonSelf != null) buttonSelf.interactable = true;
        if (selectChartButton != null) selectChartButton.interactable = true;
    }

    public async void ButtonOnClick() {
        Debug.Log("Start play button clicked");
        if (chartSelector == null) return;

        var selectedPath = chartSelector.selectedPath;
        if (selectedPath == null) {
            setState("No chart selected");
            return;
        };
        Debug.Log($"Selected path: {selectedPath}");

        if (!File.Exists(selectedPath)) {
            setState($"File not found: {selectedPath}");
            return;
        }

        setState("Loading chart...");
        disableButton();

        try {
            Exception err = await Task.Run<Exception>(() => {
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
                    return e;
                }

                return null;
            });

            if (err != null) throw err;
            else {
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
                gameMain.sasaHitClip = libSasa.load_audio_clip(ResUtils.CreateTempFile(ResUtils.ReadStreamingAsset("hit.ogg")));
                gameMain.sasaDragClip = libSasa.load_audio_clip(ResUtils.CreateTempFile(ResUtils.ReadStreamingAsset("drag.ogg")));
                gameMain.FLOW_SPEED = flowSpeedSlider.GetComponent<Slider>().value;
                MilConst.MilConst.NOTE_SIZE_SCALE = noteSizeSlider.GetComponent<Slider>().value;
                gameMain.AUTOPLAY = autoplayToggle.GetComponent<Toggle>().isOn;
                hubCanvas.gameObject.SetActive(false);

                setState("Chart loaded");
                gameMain.IntoPlay();
            }
        } catch (Exception e) {
            setState($"Error: {e.Message}");
            Debug.Log($"Error when loading chart: {e.Message}");
            Debug.LogException(e);
        } finally {
            enableButton();
        }
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
