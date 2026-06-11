<!-- 
  RainPlayer.vue
  
  Rain Player 的 Vue 3 组件封装
  使用方法见 VUE_INTEGRATION.md
-->

<template>
  <div class="rain-player-wrapper">
    <!-- 加载中状态 -->
    <div v-if="isLoading" class="loading-overlay">
      <div class="loading-spinner"></div>
      <p>加载中...</p>
    </div>

    <!-- 错误状态 -->
    <div v-if="error" class="error-overlay">
      <div class="error-icon">⚠️</div>
      <p class="error-message">{{ error }}</p>
      <button @click="retry" class="retry-btn">重试</button>
    </div>

    <!-- 播放器容器 -->
    <div ref="containerRef" class="rain-player-container"></div>
  </div>
</template>

<script>
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';

export default {
  name: 'RainPlayer',
  
  props: {
    /**
     * 谱面数据
     * 可以是对象或 JSON 字符串
     */
    chartData: {
      type: [Object, String],
      default: null
    },
    
    /**
     * 谱面 ZIP 文件 URL（传统方式）
     */
    chartUrl: {
      type: String,
      default: null
    },
    
    /**
     * 音频 URL
     */
    audioUrl: {
      type: String,
      default: null
    },
    
    /**
     * 音频 Blob
     */
    audioBlob: {
      type: Blob,
      default: null
    },
    
    /**
     * 封面 URL
     */
    coverUrl: {
      type: String,
      default: null
    },
    
    /**
     * 封面 Blob
     */
    coverBlob: {
      type: Blob,
      default: null
    },
    
    /**
     * WebGL 构建目录
     */
    webglBuildDir: {
      type: String,
      default: '/webgl_build_dir'
    },
    
    /**
     * 游戏选项
     */
    options: {
      type: Object,
      default: () => ({})
    }
  },
  
  emits: ['loaded', 'error', 'backToHub'],
  
  setup(props, { emit }) {
    const containerRef = ref(null);
    const isLoading = ref(true);
    const error = ref(null);
    let playerInstance = null;

    /**
     * 初始化播放器
     */
    const initPlayer = async () => {
      try {
        // 检查 RainPlayer 是否已加载
        if (!window.RainPlayer) {
          throw new Error('RainPlayer 未加载，请确保在 index.html 中引入了 rain_player.js');
        }

        // 配置 WebGL 构建目录
        window.RainPlayer.configure({
          webglBuildDir: props.webglBuildDir
        });

        // 准备实例化选项
        const instanceOptions = {
          container: containerRef.value,
          ...props.options
        };

        // 设置谱面数据源
        if (props.chartUrl) {
          instanceOptions.chartUrl = props.chartUrl;
        } else if (props.chartData) {
          instanceOptions.chartData = typeof props.chartData === 'string' 
            ? JSON.parse(props.chartData) 
            : props.chartData;
        } else {
          throw new Error('必须提供 chartUrl 或 chartData');
        }

        // 设置音频源
        if (props.audioUrl) {
          instanceOptions.audioUrl = props.audioUrl;
        } else if (props.audioBlob) {
          instanceOptions.audioBlob = props.audioBlob;
        } else if (!props.chartUrl) {
          throw new Error('使用新 API 时必须提供音频');
        }

        // 设置封面源
        if (props.coverUrl) {
          instanceOptions.coverUrl = props.coverUrl;
        } else if (props.coverBlob) {
          instanceOptions.coverBlob = props.coverBlob;
        } else if (!props.chartUrl) {
          throw new Error('使用新 API 时必须提供封面');
        }

        // 创建播放器实例
        playerInstance = window.RainPlayer.instantiate(instanceOptions);

        // 等待加载完成
        await playerInstance.waitLoaded();

        // 设置样式
        playerInstance.iframe.style.width = '100%';
        playerInstance.iframe.style.height = '100%';

        // 监听返回事件
        playerInstance.iframe.contentWindow.addEventListener(
          'rain_player_back_to_hub',
          handleBackToHub
        );

        isLoading.value = false;
        emit('loaded', playerInstance);

      } catch (err) {
        console.error('Rain Player 初始化失败:', err);
        error.value = err.message;
        isLoading.value = false;
        emit('error', err);
      }
    };

    /**
     * 处理返回 Hub 事件
     */
    const handleBackToHub = () => {
      emit('backToHub');
      cleanup();
    };

    /**
     * 清理资源
     */
    const cleanup = () => {
      if (playerInstance?.iframe) {
        playerInstance.iframe.remove();
        playerInstance = null;
      }
    };

    /**
     * 重试
     */
    const retry = () => {
      error.value = null;
      isLoading.value = true;
      cleanup();
      initPlayer();
    };

    // 组件挂载时初始化
    onMounted(() => {
      initPlayer();
    });

    // 组件卸载时清理
    onBeforeUnmount(() => {
      cleanup();
    });

    // 监听 props 变化（如果需要支持动态更新）
    watch(
      () => [props.chartData, props.chartUrl, props.audioUrl, props.coverUrl],
      () => {
        if (playerInstance) {
          cleanup();
          isLoading.value = true;
          error.value = null;
          initPlayer();
        }
      },
      { deep: true }
    );

    return {
      containerRef,
      isLoading,
      error,
      retry
    };
  }
};
</script>

<style scoped>
.rain-player-wrapper {
  width: 100%;
  height: 100%;
  position: relative;
  overflow: hidden;
}

.rain-player-container {
  width: 100%;
  height: 100%;
}

.loading-overlay,
.error-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  background: rgba(0, 0, 0, 0.8);
  color: white;
  z-index: 1000;
}

.loading-spinner {
  width: 50px;
  height: 50px;
  border: 4px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 20px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-icon {
  font-size: 48px;
  margin-bottom: 20px;
}

.error-message {
  font-size: 16px;
  margin-bottom: 20px;
  text-align: center;
  padding: 0 20px;
}

.retry-btn {
  padding: 10px 30px;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 5px;
  cursor: pointer;
  font-size: 16px;
  transition: all 0.3s;
}

.retry-btn:hover {
  background: #5568d3;
  transform: translateY(-2px);
}
</style>
