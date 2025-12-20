# Rain Player WebGL API 文档

## 概述

Rain Player 现在支持两种加载方式：

1. **传统方式**：通过 `chartUrl` 参数加载 ZIP 格式的谱面文件
2. **新 API 方式**：直接通过 JavaScript API 传入 JSON、封面图和音频数据

## API 使用方式

### 方式一：传统 ZIP 文件加载（向后兼容）

```javascript
const ins = RainPlayer.instantiate({
    chartUrl: "/chart.zip",
    container: document.body,
    autoPlay: false,
    noteSize: 1.15,
    // ...其他参数
});
```

### 方式二：直接传入资源（新功能）

```javascript
const ins = RainPlayer.instantiate({
    // 方式 2A: 传入 JSON 字符串
    chartJson: '{"fmt":1,"meta":{...},"bpms":[...],"lines":[...]}',
    
    // 或者方式 2B: 传入 JavaScript 对象（会自动转换为 JSON）
    chartData: {
        fmt: 1,
        meta: { /* ... */ },
        bpms: [ /* ... */ ],
        lines: [ /* ... */ ]
    },
    
    // 音频：支持 URL、Blob 或 ArrayBuffer
    audioUrl: "/path/to/audio.mp3",        // 选项1：URL
    // audioBlob: audioBlob,                // 选项2：Blob 对象
    // audioData: audioArrayBuffer,         // 选项3：ArrayBuffer
    
    // 封面：支持 URL、Blob 或 ArrayBuffer
    coverUrl: "/path/to/cover.jpg",        // 选项1：URL
    // coverBlob: coverBlob,                // 选项2：Blob 对象
    // coverData: coverArrayBuffer,         // 选项3：ArrayBuffer
    
    container: document.body,
    autoPlay: false,
    noteSize: 1.15,
    // ...其他参数
});
```

## 参数说明

### 必需参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `container` | HTMLElement | 用于放置 iframe 的容器元素 |
| `chartUrl` 或 `chartJson/chartData` | String/Object | 谱面数据源 |

### 音频和封面参数

使用新 API 方式时，必须提供以下参数：

| 参数 | 类型 | 说明 |
|------|------|------|
| `audioUrl` | String | 音频文件 URL |
| `audioBlob` | Blob | 音频文件 Blob 对象 |
| `audioData` | ArrayBuffer | 音频文件二进制数据 |
| `coverUrl` | String | 封面图片 URL |
| `coverBlob` | Blob | 封面图片 Blob 对象 |
| `coverData` | ArrayBuffer | 封面图片二进制数据 |

注意：音频和封面各选择一种方式即可（URL、Blob 或 Data）。

### 游戏设置参数（可选）

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `flowSpeed` | Number | - | 下落速度 |
| `noteSize` | Number | - | 音符大小 |
| `offset` | Number | - | 音频偏移（毫秒） |
| `speed` | Number | - | 播放速度 |
| `musicVol` | Number | - | 音乐音量 (0-1) |
| `hitsoundVol` | Number | - | 打击音量 (0-1) |
| `autoPlay` | Boolean | - | 自动播放 |
| `debug` | Boolean | - | 调试模式 |
| `chordHL` | Boolean | - | 和弦高亮 |
| `elIndicator` | Boolean | - | Early/Late 指示器 |
| `showTouchPoint` | Boolean | - | 显示触摸点 |
| `oklchColorInterplate` | Boolean | - | OKLCH 颜色插值 |
| `comboText` | String | - | Combo 文字 |

## 完整示例

### 示例 1：使用 URL 加载资源

```javascript
RainPlayer.configure({
    webglBuildDir: "./webgl_build_dir"
});

const ins = RainPlayer.instantiate({
    chartJson: JSON.stringify(chartData),
    audioUrl: "https://example.com/music.mp3",
    coverUrl: "https://example.com/cover.jpg",
    container: document.getElementById("player-container"),
    autoPlay: false,
    noteSize: 1.15,
    musicVol: 0.8,
    hitsoundVol: 0.6
});

await ins.waitLoaded();
console.log("Player loaded!");
```

### 示例 2：使用 Blob 对象

```javascript
// 假设你通过文件上传或其他方式获得了 Blob 对象
const chartJsonStr = JSON.stringify(chartData);
const audioBlob = new Blob([audioArrayBuffer], { type: 'audio/mpeg' });
const coverBlob = new Blob([coverArrayBuffer], { type: 'image/jpeg' });

const ins = RainPlayer.instantiate({
    chartJson: chartJsonStr,
    audioBlob: audioBlob,
    coverBlob: coverBlob,
    container: document.body,
    autoPlay: true
});
```

### 示例 3：从 API 获取数据

```javascript
async function loadAndPlay() {
    // 从你的后端 API 获取数据
    const response = await fetch('/api/chart/123');
    const data = await response.json();
    
    const ins = RainPlayer.instantiate({
        chartData: data.chart,  // 直接传入对象
        audioUrl: data.audioUrl,
        coverUrl: data.coverUrl,
        container: document.getElementById("game"),
        noteSize: 1.2
    });
    
    await ins.waitLoaded();
}
```

## 事件监听

```javascript
const ins = RainPlayer.instantiate({ /* ... */ });

await ins.waitLoaded();

// 监听返回 Hub 事件
ins.iframe.contentWindow.addEventListener("rain_player_back_to_hub", () => {
    console.log("Player returned to hub");
    ins.iframe.remove();
});
```

## 注意事项

1. **向后兼容**：旧的 `chartUrl` 方式仍然完全支持
2. **性能**：使用新 API 方式可以避免 ZIP 解压，加载速度更快
3. **CORS**：使用 URL 方式时，确保资源服务器配置了正确的 CORS 头
4. **格式**：音频支持浏览器支持的格式（MP3、OGG、WAV 等），封面支持 JPG、PNG
5. **Storyboard**：使用新 API 方式时，暂不支持 storyboard 图片资源
