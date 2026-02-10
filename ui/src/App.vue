<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

// API 基础路径
// 开发时 Vite proxy 将 /api 转发到后端
// 生产环境中 LPK 路由直接将 /api/ 代理到后端
const api = axios.create({ baseURL: '/api' })

// 响应式状态
const todos = ref([])
const newTodo = ref('')
const loading = ref(false)
const editingId = ref(null)
const editingText = ref('')

// 获取所有待办
async function fetchTodos() {
  loading.value = true
  try {
    const { data } = await api.get('/todos?limit=100')
    todos.value = data
  } catch (err) {
    console.error('获取待办失败:', err)
  } finally {
    loading.value = false
  }
}

// 创建待办
async function addTodo() {
  const text = newTodo.value.trim()
  if (!text) return
  try {
    const { data } = await api.post('/todos', {
      todo: text,
      isCompleted: false,
    })
    todos.value.push(data)
    newTodo.value = ''
  } catch (err) {
    console.error('创建待办失败:', err)
  }
}

// 切换完成状态
async function toggleTodo(item) {
  try {
    const { data } = await api.put(`/todos/${item.id}`, {
      todo: item.todo,
      isCompleted: !item.isCompleted,
    })
    const idx = todos.value.findIndex((t) => t.id === item.id)
    if (idx !== -1) todos.value[idx] = data
  } catch (err) {
    console.error('更新待办失败:', err)
  }
}

// 开始编辑
function startEdit(item) {
  editingId.value = item.id
  editingText.value = item.todo
}

// 保存编辑
async function saveEdit(item) {
  const text = editingText.value.trim()
  if (!text) return
  try {
    const { data } = await api.put(`/todos/${item.id}`, {
      todo: text,
      isCompleted: item.isCompleted,
    })
    const idx = todos.value.findIndex((t) => t.id === item.id)
    if (idx !== -1) todos.value[idx] = data
  } catch (err) {
    console.error('保存编辑失败:', err)
  }
  editingId.value = null
  editingText.value = ''
}

// 取消编辑
function cancelEdit() {
  editingId.value = null
  editingText.value = ''
}

// 删除待办
async function deleteTodo(item) {
  try {
    await api.delete(`/todos/${item.id}`)
    todos.value = todos.value.filter((t) => t.id !== item.id)
  } catch (err) {
    console.error('删除待办失败:', err)
  }
}

// 页面加载时获取数据
onMounted(fetchTodos)
</script>

<template>
  <div class="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-purple-50 py-8 px-4">
    <div class="max-w-xl mx-auto">
      <!-- 标题 -->
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-gray-800 mb-1">
          待办清单
        </h1>
        <p class="text-sm text-gray-400">
          .NET 10 + Vue 3 · 懒猫微服 LPK 应用
        </p>
      </div>

      <!-- 输入区域 -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-4 mb-6">
        <form @submit.prevent="addTodo" class="flex gap-3">
          <input
            v-model="newTodo"
            type="text"
            placeholder="添加新的待办事项..."
            class="flex-1 px-4 py-2.5 border border-gray-200 rounded-lg
                   focus:outline-none focus:ring-2 focus:ring-indigo-300 focus:border-indigo-300
                   text-gray-700 placeholder-gray-400 transition-all"
          />
          <button
            type="submit"
            class="px-5 py-2.5 bg-indigo-500 text-white rounded-lg font-medium
                   hover:bg-indigo-600 active:bg-indigo-700
                   transition-colors shadow-sm"
          >
            添加
          </button>
        </form>
      </div>

      <!-- 加载状态 -->
      <div v-if="loading" class="text-center py-12 text-gray-400">
        加载中...
      </div>

      <!-- 空状态 -->
      <div v-else-if="todos.length === 0" class="text-center py-12">
        <div class="text-5xl mb-3">📝</div>
        <p class="text-gray-400">还没有待办事项，添加一个吧</p>
      </div>

      <!-- 待办列表 -->
      <div v-else class="space-y-3">
        <TransitionGroup name="list">
          <div
            v-for="item in todos"
            :key="item.id"
            class="bg-white rounded-xl shadow-sm border border-gray-100 p-4
                   flex items-center gap-3 group hover:shadow-md transition-all"
          >
            <!-- 复选框 -->
            <button
              @click="toggleTodo(item)"
              class="flex-shrink-0 w-6 h-6 rounded-full border-2 flex items-center justify-center transition-colors"
              :class="item.isCompleted
                ? 'bg-green-500 border-green-500 text-white'
                : 'border-gray-300 hover:border-indigo-400'"
            >
              <svg v-if="item.isCompleted" class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
              </svg>
            </button>

            <!-- 编辑模式 -->
            <template v-if="editingId === item.id">
              <input
                v-model="editingText"
                @keyup.enter="saveEdit(item)"
                @keyup.escape="cancelEdit"
                class="flex-1 px-3 py-1.5 border border-indigo-300 rounded-lg
                       focus:outline-none focus:ring-2 focus:ring-indigo-300 text-gray-700"
                autofocus
              />
              <button
                @click="saveEdit(item)"
                class="px-3 py-1.5 text-sm bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors"
              >
                保存
              </button>
              <button
                @click="cancelEdit"
                class="px-3 py-1.5 text-sm bg-gray-100 text-gray-600 rounded-lg hover:bg-gray-200 transition-colors"
              >
                取消
              </button>
            </template>

            <!-- 显示模式 -->
            <template v-else>
              <span
                class="flex-1 text-gray-700 transition-all cursor-pointer"
                :class="{ 'line-through text-gray-400': item.isCompleted }"
                @dblclick="startEdit(item)"
              >
                {{ item.todo }}
              </span>

              <!-- 操作按钮（hover 显示） -->
              <div class="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                <button
                  @click="startEdit(item)"
                  class="p-1.5 text-gray-400 hover:text-indigo-500 hover:bg-indigo-50 rounded-lg transition-colors"
                  title="编辑"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button
                  @click="deleteTodo(item)"
                  class="p-1.5 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors"
                  title="删除"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </template>
          </div>
        </TransitionGroup>

        <!-- 统计 -->
        <div class="text-center pt-4 text-sm text-gray-400">
          共 {{ todos.length }} 项，已完成 {{ todos.filter(t => t.isCompleted).length }} 项
        </div>
      </div>

      <!-- 技术栈标注 -->
      <div class="text-center mt-10 text-xs text-gray-300">
        ASP.NET Core 10.0 Minimal API · EF Core 10 · SQLite · Vue 3 · Tailwind CSS
      </div>
    </div>
  </div>
</template>

<style>
.list-enter-active,
.list-leave-active {
  transition: all 0.3s ease;
}
.list-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}
.list-leave-to {
  opacity: 0;
  transform: translateX(30px);
}
</style>
