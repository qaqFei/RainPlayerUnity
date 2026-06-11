# Rain Player Vue.js 集成指南

## 快速开始

本指南展示如何在 Vue 3 + JavaScript 项目中集成 Rain Player。

## 安装步骤

### 1. 复制资源文件

将以下文件复制到你的 Vue 项目中：

```
your-vue-project/
  public/
    rain_player.js          # 从 WebInterface/rain_player.js 复制
    webgl_build_dir/        # WebGL 构建产物目录
      index.html
      Build/
      TemplateData/
```

### 2. 在 HTML 中引入

编辑 `public/index.html`，在 `<body>` 标签结束前添加：

```html
<script src="/rain_player.js"></script>
```

### 3. 创建 Vue 组件

#### 方式 A：基础组件（直接 API）

创建 `src/components/RainPlayer.vue`：

```vue
<template>
  <div ref="containerRef" class="rain-player-container"></div>
</template>

<script>
import { ref, onMounted, onBeforeUnmount } from 'vue';

export default {
  name: 'RainPlayer',
  props: {
    chartData: {
      type: Object,
      required: true
    },
    audioUrl: {
      type: String,
      required: true
    },
    coverUrl: {
      type: String,
      required: true
    },
    options: {
      type: Object,
      default: () => ({})
    }
  },
  setup(props) {
    const containerRef = ref(null);
    let playerInstance = null;

    onMounted(async () => {
      // 确保 RainPlayer 已加载
      if (!window.RainPlayer) {
        console.error('RainPlayer not loaded');
        return;
      }

      // 配置 WebGL 构建目录
      window.RainPlayer.configure({
        webglBuildDir: '/webgl_build_dir'
      });

      // 创建播放器实例
      playerInstance = window.RainPlayer.instantiate({
        chartData: props.chartData,
        audioUrl: props.audioUrl,
        coverUrl: props.coverUrl,
        container: containerRef.value,
        autoPlay: props.options.autoPlay ?? false,
        noteSize: props.options.noteSize ?? 1.15,
        musicVol: props.options.musicVol ?? 1.0,
        hitsoundVol: props.options.hitsoundVol ?? 1.0,
        ...props.options
      });

      try {
        await playerInstance.waitLoaded();
        console.log('Rain Player loaded successfully');
        
        // 设置 iframe 样式
        playerInstance.iframe.style.width = '100%';
        playerInstance.iframe.style.height = '100%';

        // 监听事件
        playerInstance.iframe.contentWindow.addEventListener(
          'rain_player_back_to_hub',
          handleBackToHub
        );
      } catch (error) {
        console.error('Failed to load Rain Player:', error);
      }
    });

    const handleBackToHub = () => {
      console.log('Player returned to hub');
      if (playerInstance && playerInstance.iframe) {
        playerInstance.iframe.remove();
      }
    };

    onBeforeUnmount(() => {
      if (playerInstance && playerInstance.iframe) {
        playerInstance.iframe.remove();
      }
    });

    return {
      containerRef
    };
  }
};
</script>

<style scoped>
.rain-player-container {
  width: 100%;
  height: 100%;
  position: relative;
}
</style>
```

#### 方式 B：使用 Composable（推荐）

创建 `src/composables/useRainPlayer.js`：

```javascript
import { ref, onMounted, onBeforeUnmount } from 'vue';

export function useRainPlayer(options) {
  const containerRef = ref(null);
  const isLoading = ref(true);
  const error = ref(null);
  let playerInstance = null;

  const initPlayer = async () => {
    if (!window.RainPlayer) {
      error.value = 'RainPlayer not loaded';
      return;
    }

    try {
      window.RainPlayer.configure({
        webglBuildDir: options.webglBuildDir || '/webgl_build_dir'
      });

      playerInstance = window.RainPlayer.instantiate({
        container: containerRef.value,
        ...options
      });

      await playerInstance.waitLoaded();
      
      playerInstance.iframe.style.width = '100%';
      playerInstance.iframe.style.height = '100%';

      if (options.onBackToHub) {
        playerInstance.iframe.contentWindow.addEventListener(
          'rain_player_back_to_hub',
          options.onBackToHub
        );
      }

      isLoading.value = false;
    } catch (err) {
      error.value = err.message;
      isLoading.value = false;
    }
  };

  onMounted(() => {
    initPlayer();
  });

  onBeforeUnmount(() => {
    if (playerInstance?.iframe) {
      playerInstance.iframe.remove();
    }
  });

  return {
    containerRef,
    isLoading,
    error,
    playerInstance
  };
}
```

使用 Composable 的组件：

```vue
<template>
  <div class="player-wrapper">
    <div v-if="isLoading" class="loading">加载中...</div>
    <div v-if="error" class="error">{{ error }}</div>
    <div ref="containerRef" class="rain-player-container"></div>
  </div>
</template>

<script>
import { useRainPlayer } from '@/composables/useRainPlayer';

export default {
  name: 'GameView',
  props: ['chartData', 'audioUrl', 'coverUrl'],
  setup(props) {
    const { containerRef, isLoading, error } = useRainPlayer({
      chartData: props.chartData,
      audioUrl: props.audioUrl,
      coverUrl: props.coverUrl,
      autoPlay: false,
      noteSize: 1.15,
      onBackToHub: () => {
        console.log('Back to hub');
        // 在这里处理返回逻辑，例如路由跳转
      }
    });

    return {
      containerRef,
      isLoading,
      error
    };
  }
};
</script>

<style scoped>
.player-wrapper {
  width: 100vw;
  height: 100vh;
  position: relative;
}

.rain-player-container {
  width: 100%;
  height: 100%;
}

.loading, .error {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  z-index: 1000;
}
</style>
```

## 完整应用示例

### App.vue

```vue
<template>
  <div id="app">
    <router-view />
  </div>
</template>

<script>
export default {
  name: 'App'
};
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

html, body, #app {
  width: 100%;
  height: 100%;
  overflow: hidden;
}
</style>
```

### 路由配置（router/index.js）

```javascript
import { createRouter, createWebHistory } from 'vue-router';
import HomeView from '../views/HomeView.vue';
import GameView from '../views/GameView.vue';

const routes = [
  {
    path: '/',
    name: 'Home',
    component: HomeView
  },
  {
    path: '/play/:chartId',
    name: 'Game',
    component: GameView,
    props: true
  }
];

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes
});

export default router;
```

### 视图示例（views/GameView.vue）

```vue
<template>
  <div class="game-view">
    <RainPlayer
      v-if="chartData"
      :chart-data="chartData"
      :audio-url="audioUrl"
      :cover-url="coverUrl"
      :options="playerOptions"
    />
    <div v-else class="loading">
      加载谱面数据中...
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import RainPlayer from '@/components/RainPlayer.vue';

export default {
  name: 'GameView',
  components: {
    RainPlayer
  },
  props: ['chartId'],
  setup(props) {
    const router = useRouter();
    const chartData = ref(null);
    const audioUrl = ref('');
    const coverUrl = ref('');
    const playerOptions = ref({
      autoPlay: false,
      noteSize: 1.15,
      musicVol: 0.8,
      hitsoundVol: 0.6
    });

    onMounted(async () => {
      // 从你的 API 获取谱面数据
      try {
        const response = await fetch(`/api/charts/${props.chartId}`);
        const data = await response.json();
        
        chartData.value = data.chart;
        audioUrl.value = data.audioUrl;
        coverUrl.value = data.coverUrl;
      } catch (error) {
        console.error('Failed to load chart:', error);
        // 可以跳转到错误页面或显示错误信息
      }
    });

    return {
      chartData,
      audioUrl,
      coverUrl,
      playerOptions
    };
  }
};
</script>

<style scoped>
.game-view {
  width: 100vw;
  height: 100vh;
}

.loading {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100%;
  font-size: 24px;
}
</style>
```

## 从本地文件加载

如果你想让用户上传谱面文件来游玩：

```vue
<template>
  <div>
    <input type="file" @change="handleChartFile" accept=".json" />
    <input type="file" @change="handleAudioFile" accept="audio/*" />
    <input type="file" @change="handleCoverFile" accept="image/*" />
    <button @click="startGame" :disabled="!canStart">开始游戏</button>
    
    <div v-if="showPlayer" ref="containerRef" class="player"></div>
  </div>
</template>

<script>
import { ref } from 'vue';

export default {
  setup() {
    const chartFile = ref(null);
    const audioFile = ref(null);
    const coverFile = ref(null);
    const showPlayer = ref(false);
    const containerRef = ref(null);

    const handleChartFile = (event) => {
      chartFile.value = event.target.files[0];
    };

    const handleAudioFile = (event) => {
      audioFile.value = event.target.files[0];
    };

    const handleCoverFile = (event) => {
      coverFile.value = event.target.files[0];
    };

    const canStart = ref(false);
    
    const startGame = async () => {
      const chartText = await chartFile.value.text();
      const chartData = JSON.parse(chartText);

      window.RainPlayer.configure({
        webglBuildDir: '/webgl_build_dir'
      });

      showPlayer.value = true;
      
      // 等待容器渲染
      await new Promise(resolve => setTimeout(resolve, 100));

      const player = window.RainPlayer.instantiate({
        chartData: chartData,
        audioBlob: audioFile.value,
        coverBlob: coverFile.value,
        container: containerRef.value,
        autoPlay: false
      });

      await player.waitLoaded();
      player.iframe.style.width = '100%';
      player.iframe.style.height = '100vh';
    };

    return {
      handleChartFile,
      handleAudioFile,
      handleCoverFile,
      startGame,
      canStart,
      showPlayer,
      containerRef
    };
  }
};
</script>
```

## 注意事项

1. **引入顺序**：确保 `rain_player.js` 在 Vue 应用初始化前加载
2. **路径配置**：根据你的项目结构调整 `webglBuildDir` 路径
3. **样式调整**：根据需要调整播放器容器的尺寸和样式
4. **生命周期**：在组件销毁时清理 iframe
5. **错误处理**：添加适当的错误处理和加载状态

## TypeScript 支持

如果使用 TypeScript，创建类型定义文件 `src/types/rain-player.d.ts`：

```typescript
interface RainPlayerOptions {
  chartUrl?: string;
  chartJson?: string;
  chartData?: object;
  audioUrl?: string;
  audioBlob?: Blob;
  audioData?: ArrayBuffer;
  coverUrl?: string;
  coverBlob?: Blob;
  coverData?: ArrayBuffer;
  container: HTMLElement;
  autoPlay?: boolean;
  noteSize?: number;
  offset?: number;
  speed?: number;
  musicVol?: number;
  hitsoundVol?: number;
  debug?: boolean;
  chordHL?: boolean;
  elIndicator?: boolean;
  showTouchPoint?: boolean;
  oklchColorInterplate?: boolean;
  comboText?: string;
  flowSpeed?: number;
}

interface RainPlayerInstance {
  iframe: HTMLIFrameElement;
  waitLoaded(): Promise<void>;
}

interface RainPlayer {
  configure(config: { webglBuildDir: string }): void;
  instantiate(options: RainPlayerOptions): RainPlayerInstance;
}

declare global {
  interface Window {
    RainPlayer: RainPlayer;
  }
}

export {};
```

## 更多示例

访问项目的 `WebInterface/` 目录查看更多示例和文档。
