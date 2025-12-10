# ?? Ejemplo de UI para Selección de Destinatarios

## Componente Vue.js Completo

```vue
<template>
  <div class="resumen-email-container">
    <v-card>
      <v-card-title class="headline">
        <v-icon left color="primary">mdi-email-multiple</v-icon>
        Enviar Resumen Diario de Alertas SLA
      </v-card-title>

      <v-card-text>
        <!-- Estado de carga -->
        <v-progress-linear v-if="cargando" indeterminate color="primary"></v-progress-linear>

        <!-- Lista de usuarios -->
        <div v-else>
          <v-alert type="info" outlined dense class="mb-4">
            Selecciona los administradores y analistas que recibirán el resumen diario de alertas críticas y altas.
          </v-alert>

          <!-- Botones de acción rápida -->
          <div class="mb-4">
            <v-btn small text @click="seleccionarTodos" class="mr-2">
              <v-icon left small>mdi-checkbox-marked</v-icon>
              Seleccionar Todos
            </v-btn>
            <v-btn small text @click="deseleccionarTodos">
              <v-icon left small>mdi-checkbox-blank-outline</v-icon>
              Deseleccionar Todos
            </v-btn>
          </div>

          <!-- Tabla de usuarios -->
          <v-data-table
            v-model="seleccionados"
            :headers="headers"
            :items="usuarios"
            :items-per-page="10"
            item-key="correoCorporativo"
            show-select
            class="elevation-1"
          >
            <!-- Columna de Nombre -->
            <template v-slot:[`item.nombreCompleto`]="{ item }">
              <div class="d-flex align-center">
                <v-avatar size="32" color="primary" class="mr-2">
                  <span class="white--text text-caption">
                    {{ obtenerIniciales(item.nombreCompleto) }}
                  </span>
                </v-avatar>
                <div>
                  <div class="font-weight-medium">{{ item.nombreCompleto }}</div>
                  <div class="text-caption grey--text">{{ item.username }}</div>
                </div>
              </div>
            </template>

            <!-- Columna de Rol -->
            <template v-slot:[`item.nombreRol`]="{ item }">
              <v-chip
                small
                :color="item.idRolSistema === 1 ? 'red' : 'blue'"
                dark
              >
                <v-icon left x-small>
                  {{ item.idRolSistema === 1 ? 'mdi-shield-crown' : 'mdi-account-tie' }}
                </v-icon>
                {{ item.nombreRol }}
              </v-chip>
            </template>

            <!-- Columna de Correo -->
            <template v-slot:[`item.correoCorporativo`]="{ item }">
              <div class="d-flex align-center">
                <v-icon small left color="grey">mdi-email</v-icon>
                {{ item.correoCorporativo }}
              </div>
            </template>
          </v-data-table>
        </div>
      </v-card-text>

      <v-divider></v-divider>

      <!-- Acciones -->
      <v-card-actions class="pa-4">
        <v-spacer></v-spacer>
        
        <v-chip
          v-if="seleccionados.length > 0"
          color="primary"
          outlined
          class="mr-3"
        >
          <v-icon left>mdi-email</v-icon>
          {{ seleccionados.length }} destinatario(s) seleccionado(s)
        </v-chip>

        <v-btn
          color="primary"
          large
          :loading="enviando"
          :disabled="seleccionados.length === 0"
          @click="enviarResumen"
        >
          <v-icon left>mdi-send</v-icon>
          Enviar Resumen Ahora
        </v-btn>
      </v-card-actions>
    </v-card>

    <!-- Dialog de resultados -->
    <v-dialog v-model="dialogResultados" max-width="600">
      <v-card>
        <v-card-title :class="resultadoEnvio.exito ? 'success' : 'warning'">
          <v-icon left dark>
            {{ resultadoEnvio.exito ? 'mdi-check-circle' : 'mdi-alert-circle' }}
          </v-icon>
          {{ resultadoEnvio.exito ? '¡Resumen Enviado!' : 'Resumen Enviado Parcialmente' }}
        </v-card-title>

        <v-card-text class="pt-4">
          <v-alert
            :type="resultadoEnvio.exito ? 'success' : 'warning'"
            outlined
            prominent
          >
            {{ resultadoEnvio.mensaje }}
          </v-alert>

          <div class="mt-4">
            <div class="text-subtitle-2 mb-2">Estadísticas:</div>
            <v-row dense>
              <v-col cols="4">
                <v-card outlined>
                  <v-card-text class="text-center">
                    <div class="text-h4 primary--text">{{ resultadoEnvio.cantidadAlertas }}</div>
                    <div class="text-caption">Alertas</div>
                  </v-card-text>
                </v-card>
              </v-col>
              <v-col cols="4">
                <v-card outlined>
                  <v-card-text class="text-center">
                    <div class="text-h4 success--text">{{ enviosExitosos }}</div>
                    <div class="text-caption">Exitosos</div>
                  </v-card-text>
                </v-card>
              </v-col>
              <v-col cols="4">
                <v-card outlined>
                  <v-card-text class="text-center">
                    <div class="text-h4 error--text">{{ enviosFallidos }}</div>
                    <div class="text-caption">Fallidos</div>
                  </v-card-text>
                </v-card>
              </v-col>
            </v-row>
          </div>

          <!-- Detalle de envíos -->
          <div v-if="resultadoEnvio.resultadosEnvios" class="mt-4">
            <div class="text-subtitle-2 mb-2">Detalle por destinatario:</div>
            <v-list dense>
              <v-list-item
                v-for="(resultado, index) in resultadoEnvio.resultadosEnvios"
                :key="index"
              >
                <v-list-item-avatar>
                  <v-icon :color="resultado.exitoso ? 'success' : 'error'">
                    {{ resultado.exitoso ? 'mdi-check-circle' : 'mdi-alert-circle' }}
                  </v-icon>
                </v-list-item-avatar>
                <v-list-item-content>
                  <v-list-item-title>{{ resultado.destinatario }}</v-list-item-title>
                  <v-list-item-subtitle v-if="!resultado.exitoso" class="error--text">
                    {{ resultado.mensajeError }}
                  </v-list-item-subtitle>
                </v-list-item-content>
              </v-list-item>
            </v-list>
          </div>
        </v-card-text>

        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="primary" text @click="dialogResultados = false">
            Cerrar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script>
export default {
  name: 'ResumenEmailSelector',
  
  data() {
    return {
      cargando: false,
      enviando: false,
      usuarios: [],
      seleccionados: [],
      dialogResultados: false,
      resultadoEnvio: {},
      
      headers: [
        { text: 'Nombre', value: 'nombreCompleto', sortable: true },
        { text: 'Rol', value: 'nombreRol', sortable: true },
        { text: 'Correo Corporativo', value: 'correoCorporativo', sortable: true }
      ]
    };
  },

  computed: {
    enviosExitosos() {
      if (!this.resultadoEnvio.resultadosEnvios) return 0;
      return this.resultadoEnvio.resultadosEnvios.filter(r => r.exitoso).length;
    },
    
    enviosFallidos() {
      if (!this.resultadoEnvio.resultadosEnvios) return 0;
      return this.resultadoEnvio.resultadosEnvios.filter(r => !r.exitoso).length;
    }
  },

  mounted() {
    this.cargarUsuarios();
  },

  methods: {
    async cargarUsuarios() {
      this.cargando = true;
      try {
        const response = await this.$axios.get('/api/email/administradores-analistas');
        
        if (response.data.success) {
          this.usuarios = response.data.usuarios;
          
          this.$notify({
            type: 'success',
            title: 'Usuarios Cargados',
            text: `Se encontraron ${response.data.total} administradores y analistas`
          });
        }
      } catch (error) {
        console.error('Error al cargar usuarios:', error);
        this.$notify({
          type: 'error',
          title: 'Error',
          text: 'No se pudieron cargar los usuarios'
        });
      } finally {
        this.cargando = false;
      }
    },

    async enviarResumen() {
      if (this.seleccionados.length === 0) {
        this.$notify({
          type: 'warning',
          title: 'Advertencia',
          text: 'Selecciona al menos un destinatario'
        });
        return;
      }

      const correosSeleccionados = this.seleccionados.map(u => u.correoCorporativo);

      this.enviando = true;
      try {
        const response = await this.$axios.post('/api/email/send-summary-multiple', {
          destinatarios: correosSeleccionados
        });

        if (response.data.success) {
          this.resultadoEnvio = response.data.data;
          this.dialogResultados = true;
        }
      } catch (error) {
        console.error('Error al enviar resumen:', error);
        this.$notify({
          type: 'error',
          title: 'Error',
          text: 'No se pudo enviar el resumen. Intenta nuevamente.'
        });
      } finally {
        this.enviando = false;
      }
    },

    seleccionarTodos() {
      this.seleccionados = [...this.usuarios];
    },

    deseleccionarTodos() {
      this.seleccionados = [];
    },

    obtenerIniciales(nombreCompleto) {
      if (!nombreCompleto) return '??';
      const palabras = nombreCompleto.split(' ');
      return palabras
        .slice(0, 2)
        .map(p => p[0])
        .join('')
        .toUpperCase();
    }
  }
};
</script>

<style scoped>
.resumen-email-container {
  padding: 20px;
}
</style>
```

---

## Ejemplo con HTML/JS Puro (Sin frameworks)

```html
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Enviar Resumen Diario</title>
  <style>
    * {
      margin: 0;
      padding: 0;
      box-sizing: border-box;
    }

    body {
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      background: #f5f5f5;
      padding: 20px;
    }

    .container {
      max-width: 900px;
      margin: 0 auto;
      background: white;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      overflow: hidden;
    }

    .header {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 20px 30px;
    }

    .header h1 {
      font-size: 24px;
      margin-bottom: 5px;
    }

    .header p {
      opacity: 0.9;
      font-size: 14px;
    }

    .content {
      padding: 30px;
    }

    .actions {
      display: flex;
      gap: 10px;
      margin-bottom: 20px;
    }

    button {
      padding: 10px 20px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s;
    }

    .btn-primary {
      background: #667eea;
      color: white;
    }

    .btn-primary:hover {
      background: #5568d3;
    }

    .btn-secondary {
      background: #e0e0e0;
      color: #333;
    }

    .btn-secondary:hover {
      background: #d0d0d0;
    }

    .btn-send {
      background: #4caf50;
      color: white;
      font-size: 16px;
      padding: 12px 30px;
    }

    .btn-send:hover {
      background: #45a049;
    }

    .btn-send:disabled {
      background: #ccc;
      cursor: not-allowed;
    }

    .usuarios-table {
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 20px;
    }

    .usuarios-table th,
    .usuarios-table td {
      padding: 12px;
      text-align: left;
      border-bottom: 1px solid #e0e0e0;
    }

    .usuarios-table th {
      background: #f8f9fa;
      font-weight: 600;
      color: #333;
    }

    .usuarios-table tr:hover {
      background: #f8f9fa;
    }

    .usuario-checkbox {
      width: 18px;
      height: 18px;
      cursor: pointer;
    }

    .rol-badge {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
      color: white;
    }

    .rol-admin {
      background: #d32f2f;
    }

    .rol-analista {
      background: #1976d2;
    }

    .footer {
      padding: 20px 30px;
      background: #f8f9fa;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .selected-count {
      font-size: 14px;
      color: #666;
    }

    .loading {
      text-align: center;
      padding: 40px;
      color: #666;
    }

    .alert {
      padding: 15px;
      border-radius: 4px;
      margin-bottom: 20px;
    }

    .alert-success {
      background: #d4edda;
      border: 1px solid #c3e6cb;
      color: #155724;
    }

    .alert-error {
      background: #f8d7da;
      border: 1px solid #f5c6cb;
      color: #721c24;
    }

    .modal {
      display: none;
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0,0,0,0.5);
      justify-content: center;
      align-items: center;
      z-index: 1000;
    }

    .modal.active {
      display: flex;
    }

    .modal-content {
      background: white;
      border-radius: 8px;
      padding: 30px;
      max-width: 500px;
      width: 90%;
      max-height: 80vh;
      overflow-y: auto;
    }

    .modal-header {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 20px;
    }

    .modal-header h2 {
      margin: 0;
    }

    .resultado-item {
      padding: 10px;
      border-left: 4px solid;
      margin-bottom: 10px;
      background: #f8f9fa;
    }

    .resultado-item.exitoso {
      border-color: #4caf50;
    }

    .resultado-item.fallido {
      border-color: #f44336;
    }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <h1>?? Enviar Resumen Diario de Alertas SLA</h1>
      <p>Selecciona los administradores y analistas que recibirán el resumen</p>
    </div>

    <div class="content">
      <!-- Estado de carga -->
      <div id="loading" class="loading" style="display: none;">
        <p>Cargando usuarios...</p>
      </div>

      <!-- Contenido principal -->
      <div id="main-content" style="display: none;">
        <!-- Acciones rápidas -->
        <div class="actions">
          <button class="btn-secondary" onclick="seleccionarTodos()">
            ? Seleccionar Todos
          </button>
          <button class="btn-secondary" onclick="deseleccionarTodos()">
            ? Deseleccionar Todos
          </button>
        </div>

        <!-- Tabla de usuarios -->
        <table class="usuarios-table">
          <thead>
            <tr>
              <th width="50">
                <input type="checkbox" id="checkbox-all" onchange="toggleTodos()">
              </th>
              <th>Nombre Completo</th>
              <th>Rol</th>
              <th>Correo Corporativo</th>
            </tr>
          </thead>
          <tbody id="usuarios-tbody">
            <!-- Se llena dinámicamente -->
          </tbody>
        </table>
      </div>
    </div>

    <div class="footer">
      <div class="selected-count">
        <span id="selected-count">0</span> destinatario(s) seleccionado(s)
      </div>
      <button
        id="btn-enviar"
        class="btn-send"
        onclick="enviarResumen()"
        disabled
      >
        Enviar Resumen Ahora
      </button>
    </div>
  </div>

  <!-- Modal de resultados -->
  <div id="modal-resultados" class="modal">
    <div class="modal-content">
      <div class="modal-header">
        <span id="modal-icon">?</span>
        <h2 id="modal-title">Resultados del Envío</h2>
      </div>
      <div id="modal-body">
        <!-- Se llena dinámicamente -->
      </div>
      <button class="btn-primary" onclick="cerrarModal()">Cerrar</button>
    </div>
  </div>

  <script>
    const API_BASE_URL = 'https://tu-api.com/api'; // Cambia esto por tu URL
    let usuarios = [];

    // Cargar usuarios al iniciar
    document.addEventListener('DOMContentLoaded', () => {
      cargarUsuarios();
    });

    async function cargarUsuarios() {
      const loading = document.getElementById('loading');
      const mainContent = document.getElementById('main-content');
      
      loading.style.display = 'block';
      mainContent.style.display = 'none';

      try {
        const response = await fetch(`${API_BASE_URL}/email/administradores-analistas`);
        const data = await response.json();

        if (data.success) {
          usuarios = data.usuarios;
          renderizarUsuarios();
          mainContent.style.display = 'block';
        }
      } catch (error) {
        console.error('Error al cargar usuarios:', error);
        alert('Error al cargar usuarios');
      } finally {
        loading.style.display = 'none';
      }
    }

    function renderizarUsuarios() {
      const tbody = document.getElementById('usuarios-tbody');
      tbody.innerHTML = '';

      usuarios.forEach((usuario, index) => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
          <td>
            <input
              type="checkbox"
              class="usuario-checkbox"
              data-index="${index}"
              data-correo="${usuario.correoCorporativo}"
              onchange="actualizarContador()"
            >
          </td>
          <td>${usuario.nombreCompleto}</td>
          <td>
            <span class="rol-badge ${usuario.idRolSistema === 1 ? 'rol-admin' : 'rol-analista'}">
              ${usuario.nombreRol}
            </span>
          </td>
          <td>${usuario.correoCorporativo}</td>
        `;
        tbody.appendChild(tr);
      });

      actualizarContador();
    }

    function seleccionarTodos() {
      document.querySelectorAll('.usuario-checkbox').forEach(cb => cb.checked = true);
      document.getElementById('checkbox-all').checked = true;
      actualizarContador();
    }

    function deseleccionarTodos() {
      document.querySelectorAll('.usuario-checkbox').forEach(cb => cb.checked = false);
      document.getElementById('checkbox-all').checked = false;
      actualizarContador();
    }

    function toggleTodos() {
      const checkboxAll = document.getElementById('checkbox-all');
      document.querySelectorAll('.usuario-checkbox').forEach(cb => {
        cb.checked = checkboxAll.checked;
      });
      actualizarContador();
    }

    function actualizarContador() {
      const checkboxes = document.querySelectorAll('.usuario-checkbox:checked');
      const count = checkboxes.length;
      
      document.getElementById('selected-count').textContent = count;
      document.getElementById('btn-enviar').disabled = count === 0;
    }

    async function enviarResumen() {
      const checkboxes = document.querySelectorAll('.usuario-checkbox:checked');
      const correosSeleccionados = Array.from(checkboxes).map(cb => cb.dataset.correo);

      if (correosSeleccionados.length === 0) {
        alert('Selecciona al menos un destinatario');
        return;
      }

      const btnEnviar = document.getElementById('btn-enviar');
      btnEnviar.disabled = true;
      btnEnviar.textContent = 'Enviando...';

      try {
        const response = await fetch(`${API_BASE_URL}/email/send-summary-multiple`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            destinatarios: correosSeleccionados
          })
        });

        const data = await response.json();

        if (data.success) {
          mostrarResultados(data.data);
        }
      } catch (error) {
        console.error('Error al enviar resumen:', error);
        alert('Error al enviar resumen. Intenta nuevamente.');
      } finally {
        btnEnviar.disabled = false;
        btnEnviar.textContent = 'Enviar Resumen Ahora';
      }
    }

    function mostrarResultados(resultado) {
      const modal = document.getElementById('modal-resultados');
      const modalBody = document.getElementById('modal-body');
      const modalTitle = document.getElementById('modal-title');
      const modalIcon = document.getElementById('modal-icon');

      modalTitle.textContent = resultado.exito ? '? Resumen Enviado' : '? Resumen Enviado Parcialmente';
      modalIcon.textContent = resultado.exito ? '?' : '?';

      let html = `
        <div class="alert ${resultado.exito ? 'alert-success' : 'alert-error'}">
          ${resultado.mensaje}
        </div>
        <p><strong>Alertas incluidas:</strong> ${resultado.cantidadAlertas}</p>
        <hr>
        <h3>Detalle de envíos:</h3>
      `;

      if (resultado.resultadosEnvios) {
        resultado.resultadosEnvios.forEach(r => {
          html += `
            <div class="resultado-item ${r.exitoso ? 'exitoso' : 'fallido'}">
              <strong>${r.destinatario}</strong>
              ${r.exitoso ? '? Enviado' : `? Error: ${r.mensajeError}`}
            </div>
          `;
        });
      }

      modalBody.innerHTML = html;
      modal.classList.add('active');
    }

    function cerrarModal() {
      document.getElementById('modal-resultados').classList.remove('active');
    }
  </script>
</body>
</html>
```

---

## ?? Notas de Implementación

1. **Reemplaza `API_BASE_URL`** con la URL real de tu backend.

2. **Autenticación:** Si tu API requiere autenticación JWT, agrega el header:
   ```javascript
   headers: {
     'Content-Type': 'application/json',
     'Authorization': `Bearer ${token}`
   }
   ```

3. **Personalización:** Los estilos y componentes son adaptables a tu framework actual (Vue, React, Angular, etc.).

4. **Validación:** El frontend valida que haya al menos un destinatario antes de enviar.

5. **Feedback:** Muestra resultados detallados con éxito/error por cada destinatario.

---

¡Listo para integrar en tu proyecto! ??
