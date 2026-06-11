# Rain Player Unity

这是一个使用 Unity 开发的 Milthm 自制谱面游玩器。

## 功能特性

- 支持 Milthm 格式谱面
- WebGL 平台支持，可在浏览器中运行
- **新功能**：支持通过 API 直接传入 JSON、音频和封面，无需打包为 ZIP
- 完整的 Vue.js 集成支持

## 在网页中嵌入

### 方式一：传统 ZIP 文件加载

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

### 方式二：直接传入资源（新功能）

无需打包 ZIP，直接通过 API 传入 JSON、音频和封面：

```html
<body></body>

<script src="/rain_player.js"></script>

<script>
    RainPlayer.configure({
        webglBuildDir: "./webgl_build_dir"
    });

    // 谱面数据
    const chartData = {
        fmt: 1,
        meta: {
            name: "My Chart",
            difficulty: 10,
            // ...
        },
        bpms: [/* ... */],
        lines: [/* ... */]
    };

    const ins = RainPlayer.instantiate({
        chartData: chartData,          // 直接传入 JavaScript 对象
        audioUrl: "/audio.mp3",        // 音频文件 URL
        coverUrl: "/cover.jpg",        // 封面图片 URL
        container: document.body,
        autoPlay: false,
        noteSize: 1.15,
        elIndicator: true,
        // ...
    });

    (async () => {
        await ins.waitLoaded();
        ins.iframe.style.width = ins.iframe.style.height = "100%";
        ins.iframe.contentWindow.addEventListener("rain_player_back_to_hub", () => {
            ins.iframe.remove();
        });
    })();
</script>
```

也可以使用 Blob 对象：

```javascript
const ins = RainPlayer.instantiate({
    chartJson: JSON.stringify(chartData),  // 或使用 JSON 字符串
    audioBlob: audioBlob,                  // Blob 对象
    coverBlob: coverBlob,                  // Blob 对象
    container: document.body,
    // ...
});
```

## Vue.js 项目集成

详细的 Vue.js 集成指南请查看 [VUE_INTEGRATION.md](WebInterface/VUE_INTEGRATION.md)。

快速示例：

```vue
<template>
  <div ref="containerRef" class="rain-player"></div>
</template>

<script>
import { ref, onMounted } from 'vue';

export default {
  props: ['chartData', 'audioUrl', 'coverUrl'],
  setup(props) {
    const containerRef = ref(null);

    onMounted(async () => {
      window.RainPlayer.configure({
        webglBuildDir: '/webgl_build_dir'
      });

      const player = window.RainPlayer.instantiate({
        chartData: props.chartData,
        audioUrl: props.audioUrl,
        coverUrl: props.coverUrl,
        container: containerRef.value,
        autoPlay: false
      });

      await player.waitLoaded();
    });

    return { containerRef };
  }
};
</script>
```

## API 文档

完整的 API 文档请查看 [API.md](WebInterface/API.md)。

支持的参数：

| 参数 | 类型 | 说明 |
|------|------|------|
| `chartUrl` | String | ZIP 格式谱面文件 URL（传统方式） |
| `chartData` | Object | 谱面 JavaScript 对象（新方式） |
| `chartJson` | String | 谱面 JSON 字符串（新方式） |
| `audioUrl` / `audioBlob` / `audioData` | String/Blob/ArrayBuffer | 音频文件 |
| `coverUrl` / `coverBlob` / `coverData` | String/Blob/ArrayBuffer | 封面图片 |
| `container` | HTMLElement | 容器元素 |
| `autoPlay` | Boolean | 自动播放 |
| `noteSize` | Number | 音符大小 |
| ...更多参数请查看 API 文档 | | |

## 许可证

请查看 LICENSE 文件。
