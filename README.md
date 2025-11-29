# Rain Player Unity

这是一个使用 Unity 开发的 Milthm 自制谱面游玩器。

## 在网页中嵌入

复制 `/WebInterface/rain_player.js` 到你的项目内, 示例:

```html
<body></body>

<script src="/rain_player.js"></script>

<script>
    console.log(RainPlayer);

    RainPlayer.configure({
        webglBuildDir: "./webgl_build_dir", // rain player webgl 构建的产物目录
    });
    console.log("configured");

    const ins = RainPlayer.instantiate({
        chartUrl: "/chart.zip", // 谱面文件地址, 这里尽量传绝对路径
        container: document.body, // 放置 iframe 的容器
        autoPlay: false,
        noteSize: 1.15,
        elIndicator: true,
        // ...
    });
    console.log("instance: ", ins);

    (async () => {
        await ins.waitLoaded();
        ins.iframe.style.width = ins.iframe.style.height = "100%";
        ins.iframe.contentWindow.addEventListener("rain_player_back_to_hub", () => {
            ins.iframe.remove();
        });
    })();
</script>
```
