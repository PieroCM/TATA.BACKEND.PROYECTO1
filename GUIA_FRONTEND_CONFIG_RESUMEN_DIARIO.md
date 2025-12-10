# ?? Guía de Integración Frontend: Configuración de Resumen Diario

## ?? Objetivo
Integrar el toggle y selector de hora del frontend Quasar con el backend para controlar el envío automático del resumen diario de alertas SLA.

---

## ?? Endpoints del Backend

### 1?? **GET: Obtener Configuración Actual**

```http
GET https://localhost:7000/api/email/config
```

**Respuesta Exitosa (200 OK):**
```json
{
  "id": 1,
  "destinatarioResumen": "admin@empresa.com",
  "envioInmediato": true,
  "resumenDiario": true,
  "horaResumen": "08:00:00",
  "creadoEn": "2024-01-01T10:00:00Z",
  "actualizadoEn": "2024-01-15T14:30:00Z"
}
```

**Campos Importantes para el Frontend:**
- `resumenDiario` (boolean): Estado del toggle (true = activado, false = desactivado)
- `horaResumen` (string "HH:mm:ss"): Hora configurada para el envío

---

### 2?? **PUT: Actualizar Configuración**

```http
PUT https://localhost:7000/api/email/config/1
Content-Type: application/json
```

**Body (Todos los campos son opcionales):**
```json
{
  "resumenDiario": true,
  "horaResumen": "08:00:00"
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "mensaje": "Configuración actualizada exitosamente",
  "data": {
    "id": 1,
    "destinatarioResumen": "admin@empresa.com",
    "envioInmediato": true,
    "resumenDiario": true,
    "horaResumen": "08:00:00",
    "creadoEn": "2024-01-01T10:00:00Z",
    "actualizadoEn": "2024-01-15T15:00:00Z"
  },
  "actualizadoEn": "2024-01-15T15:00:00Z"
}
```

**Respuesta de Error (400 Bad Request):**
```json
{
  "success": false,
  "mensaje": "Datos inválidos",
  "errores": [
    "El formato del email es inválido"
  ]
}
```

---

## ?? Casos de Uso

### **Caso 1: Activar Resumen Diario + Configurar Hora**

El usuario activa el toggle y selecciona las 08:00:00.

```json
PUT /api/email/config/1
{
  "resumenDiario": true,
  "horaResumen": "08:00:00"
}
```

### **Caso 2: Desactivar Resumen Diario**

El usuario desactiva el toggle (la hora se mantiene guardada).

```json
PUT /api/email/config/1
{
  "resumenDiario": false
}
```

### **Caso 3: Cambiar Solo la Hora**

El usuario cambia la hora sin tocar el toggle.

```json
PUT /api/email/config/1
{
  "horaResumen": "14:30:00"
}
```

### **Caso 4: Actualizar Todo a la Vez**

```json
PUT /api/email/config/1
{
  "destinatarioResumen": "nuevo@empresa.com",
  "resumenDiario": true,
  "horaResumen": "09:15:00"
}
```

---

## ?? Ejemplo de Integración con Quasar/Vue

### **Composable para Email Config**

```javascript
// src/composables/useEmailConfig.js
import { ref } from 'vue';
import { api } from 'boot/axios'; // Tu instancia de Axios configurada
import { Notify } from 'quasar';

export function useEmailConfig() {
  const config = ref(null);
  const loading = ref(false);

  // Obtener configuración actual
  const fetchConfig = async () => {
    loading.value = true;
    try {
      const response = await api.get('/email/config');
      config.value = response.data;
      return config.value;
    } catch (error) {
      Notify.create({
        type: 'negative',
        message: 'Error al cargar configuración de email',
        caption: error.response?.data?.mensaje || error.message
      });
      throw error;
    } finally {
      loading.value = false;
    }
  };

  // Actualizar configuración
  const updateConfig = async (configId, data) => {
    loading.value = true;
    try {
      const response = await api.put(`/email/config/${configId}`, data);
      
      if (response.data.success) {
        config.value = response.data.data;
        
        Notify.create({
          type: 'positive',
          message: '? Configuración actualizada',
          caption: response.data.mensaje
        });
        
        return config.value;
      }
    } catch (error) {
      Notify.create({
        type: 'negative',
        message: '? Error al actualizar configuración',
        caption: error.response?.data?.mensaje || error.message
      });
      throw error;
    } finally {
      loading.value = false;
    }
  };

  return {
    config,
    loading,
    fetchConfig,
    updateConfig
  };
}
```

### **Componente Vue con Toggle y Time Picker**

```vue
<template>
  <q-card class="q-pa-md">
    <q-card-section>
      <div class="text-h6">?? Configuración de Resumen Diario</div>
      <div class="text-caption text-grey">
        Envía un resumen consolidado de todas las alertas del día.
      </div>
    </q-card-section>

    <q-separator />

    <q-card-section>
      <!-- Toggle: Activar/Desactivar Resumen Diario -->
      <div class="row items-center q-mb-md">
        <div class="col">
          <div class="text-subtitle1">Resumen diario</div>
          <div class="text-caption text-grey">
            Envía un resumen consolidado de todas las alertas del día.
          </div>
        </div>
        <div class="col-auto">
          <q-toggle
            v-model="resumenDiarioActivo"
            color="primary"
            size="lg"
            @update:model-value="onToggleChange"
          />
        </div>
      </div>

      <!-- Time Picker: Hora de Envío -->
      <div v-if="resumenDiarioActivo" class="q-mt-md">
        <div class="text-subtitle2 q-mb-sm">? Hora de envío</div>
        <q-input
          v-model="horaResumen"
          label="Hora de envío"
          mask="time"
          :rules="['time']"
          filled
          @update:model-value="onHoraChange"
        >
          <template v-slot:prepend>
            <q-icon name="access_time" />
          </template>
          <template v-slot:append>
            <q-icon name="access_time" class="cursor-pointer">
              <q-popup-proxy cover transition-show="scale" transition-hide="scale">
                <q-time v-model="horaResumen" format24h>
                  <div class="row items-center justify-end">
                    <q-btn v-close-popup label="Cerrar" color="primary" flat />
                  </div>
                </q-time>
              </q-popup-proxy>
            </q-icon>
          </template>
        </q-input>
        <div class="text-caption text-grey q-mt-xs">
          Formato HH:mm:ss
        </div>
      </div>
    </q-card-section>

    <q-separator />

    <q-card-actions align="right">
      <q-btn
        label="?? Guardar"
        color="primary"
        :loading="loading"
        @click="guardarConfiguracion"
      />
      <q-btn
        label="?? Probar Envío"
        color="secondary"
        outline
        :disable="!resumenDiarioActivo"
        @click="probarEnvio"
      />
    </q-card-actions>
  </q-card>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useEmailConfig } from 'src/composables/useEmailConfig';
import { useQuasar } from 'quasar';

const $q = useQuasar();
const { config, loading, fetchConfig, updateConfig } = useEmailConfig();

// Estados locales
const resumenDiarioActivo = ref(false);
const horaResumen = ref('08:00:00');
const configId = ref(1);

// Cargar configuración al montar
onMounted(async () => {
  try {
    await fetchConfig();
    if (config.value) {
      resumenDiarioActivo.value = config.value.resumenDiario;
      horaResumen.value = config.value.horaResumen;
      configId.value = config.value.id;
    }
  } catch (error) {
    console.error('Error al cargar configuración:', error);
  }
});

// Handlers
const onToggleChange = (value) => {
  console.log('Toggle cambió a:', value);
  // Puedes auto-guardar aquí o esperar al botón Guardar
};

const onHoraChange = (value) => {
  console.log('Hora cambió a:', value);
  // Puedes auto-guardar aquí o esperar al botón Guardar
};

// Guardar configuración
const guardarConfiguracion = async () => {
  try {
    const payload = {
      resumenDiario: resumenDiarioActivo.value,
      horaResumen: horaResumen.value
    };

    await updateConfig(configId.value, payload);

    $q.notify({
      type: 'positive',
      message: '? Configuración guardada correctamente',
      position: 'top'
    });
  } catch (error) {
    console.error('Error al guardar:', error);
  }
};

// Probar envío manual
const probarEnvio = async () => {
  $q.dialog({
    title: '?? Prueba de Envío',
    message: '¿Deseas enviar un resumen de prueba ahora?',
    cancel: true,
    persistent: true
  }).onOk(async () => {
    try {
      // Llamar al endpoint de prueba manual
      await api.post('/email/send-summary');
      
      $q.notify({
        type: 'positive',
        message: '?? Resumen enviado correctamente',
        caption: 'Revisa tu bandeja de entrada'
      });
    } catch (error) {
      $q.notify({
        type: 'negative',
        message: '? Error al enviar resumen',
        caption: error.response?.data?.mensaje || error.message
      });
    }
  });
};
</script>
```

---

## ?? Flujo de Datos

```
???????????????????
?   Frontend      ?
?   (Quasar)      ?
???????????????????
         ?
         ? GET /api/email/config
         ?
         ?
???????????????????
?   Backend       ?
? EmailController ?
???????????????????
         ?
         ? Retorna configuración actual
         ? { resumenDiario: true, horaResumen: "08:00:00" }
         ?
         ?
???????????????????
?   Frontend      ?
? Renderiza UI    ?
? Toggle + Picker ?
???????????????????
         ?
         ? Usuario cambia valores
         ?
         ?
???????????????????
?   Frontend      ?
? PUT /api/email  ?
? /config/1       ?
???????????????????
         ?
         ? { resumenDiario: true, horaResumen: "14:00:00" }
         ?
         ?
???????????????????
?   Backend       ?
? Actualiza BD    ?
???????????????????
         ?
         ? Retorna config actualizada
         ? { success: true, data: {...} }
         ?
         ?
???????????????????
?   Frontend      ?
? Muestra notif.  ?
? ? Guardado     ?
???????????????????
```

---

## ?? Testing desde PowerShell

Ejecuta el script de prueba:

```powershell
.\Scripts\Test-ConfiguracionResumenDiario.ps1
```

---

## ? Validaciones del Backend

El backend ya valida automáticamente:

1. ? **Formato de hora válido** (HH:mm:ss entre 00:00:00 y 23:59:59)
2. ? **Tipo booleano** para `resumenDiario`
3. ? **Campos opcionales** (solo actualiza lo que se envía)
4. ? **Email válido** si se envía `destinatarioResumen`

---

## ?? Checklist de Integración

- [ ] Implementar composable `useEmailConfig.js`
- [ ] Crear componente con toggle y time picker
- [ ] Conectar con endpoint GET `/api/email/config`
- [ ] Conectar con endpoint PUT `/api/email/config/1`
- [ ] Agregar notificaciones de éxito/error
- [ ] Probar activar/desactivar toggle
- [ ] Probar cambio de hora
- [ ] Probar guardado simultáneo de ambos campos
- [ ] Validar formato de hora en frontend (opcional)
- [ ] Agregar botón "Probar Envío" (opcional)

---

## ?? Soporte

Si encuentras algún problema o necesitas ajustes adicionales en el backend, contacta al equipo de desarrollo.

**Backend preparado para:** ? .NET 9 + C# 13
**Frontend esperado:** ? Quasar + Vue 3 + Composition API
