const scanner = {
  buffer: '',
  lastKeyAt: 0,
  busy: false,
  count: 0,
  acceptedTokens: new Set()
};

const qrInput = document.querySelector('#qrInput');
const scanButton = document.querySelector('#scanButton');
const clearButton = document.querySelector('#clearButton');
const eventSelect = document.querySelector('#eventSelect');
const staffId = document.querySelector('#staffId');
const deviceName = document.querySelector('#deviceName');
const resultPanel = document.querySelector('#resultPanel');
const resultTitle = document.querySelector('#resultTitle');
const resultMessage = document.querySelector('#resultMessage');
const locationSection = document.querySelector('#locationSection');
const locationRow = document.querySelector('#locationRow');
const locationSeat = document.querySelector('#locationSeat');
const latency = document.querySelector('#latency');
const ticketId = document.querySelector('#ticketId');
const scanId = document.querySelector('#scanId');
const scanHealth = document.querySelector('#scanHealth');
const scanHealthText = document.querySelector('#scanHealthText');
const scanHistory = document.querySelector('#scanHistory');
const scanCount = document.querySelector('#scanCount');

if (qrInput && scanButton) {
  qrInput.focus();
  scanButton.addEventListener('click', () => submitScan(qrInput.value));
  clearButton?.addEventListener('click', resetInput);

  qrInput.addEventListener('keydown', (event) => {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      submitScan(qrInput.value);
    }
  });

  document.addEventListener('keydown', captureScannerInput);
}

function captureScannerInput(event) {
  const activeTag = document.activeElement?.tagName?.toLowerCase();
  const isEditing = ['input', 'textarea', 'select'].includes(activeTag);

  if (isEditing && document.activeElement !== qrInput) {
    return;
  }

  const now = performance.now();
  const fastSequence = now - scanner.lastKeyAt < 45;
  scanner.lastKeyAt = now;

  if (event.key === 'Enter') {
    if (scanner.buffer.length > 3) {
      event.preventDefault();
      qrInput.value = scanner.buffer;
      submitScan(scanner.buffer);
    }

    scanner.buffer = '';
    return;
  }

  if (event.key.length !== 1) {
    return;
  }

  scanner.buffer = fastSequence ? scanner.buffer + event.key : event.key;

  if (scanner.buffer.length > 180) {
    scanner.buffer = scanner.buffer.slice(-180);
  }
}

async function submitScan(rawPayload) {
  const payload = rawPayload.trim();

  if (scanner.busy || !payload) {
    setHealth('error', payload ? 'Validación en curso' : 'QR ilegible');
    return;
  }

  const employee = Number.parseInt(staffId.value, 10);

  if (!employee || employee < 1) {
    setHealth('error', 'Empleado requerido');
    staffId.focus();
    return;
  }

  if (scanner.acceptedTokens.has(payload)) {
    const duplicate = {
      resultado: 'DUPLICADO',
      titulo: 'QR YA ESCANEADO',
      mensaje: 'Este ticket ya fue autorizado en este punto de acceso.',
      tipoAlerta: 'QR_DUPLICADO_LOCAL',
      latenciaMs: 0,
      fechaScan: new Date().toISOString()
    };

    renderResult(duplicate);
    addHistory(duplicate);
    resetInput();
    return;
  }

  scanner.busy = true;
  scanButton.disabled = true;
  setHealth('loading', 'Validando en tiempo real');

  try {
    const response = await fetch('/access/scan', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        qrPayload: payload,
        idEmpleado: employee,
        idEvento: eventSelect.value ? Number.parseInt(eventSelect.value, 10) : null,
        dispositivo: deviceName.value || 'Scanner acceso'
      })
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.error || 'No se pudo procesar el QR.');
    }

    renderResult(data);
    addHistory(data);

    if (`${data.resultado || ''}`.toUpperCase() === 'VALIDO') {
      scanner.acceptedTokens.add(payload);
    }

    resetInput();
  } catch (error) {
    renderResult({
      resultado: 'ERROR',
      titulo: 'ACCESO DENEGADO',
      mensaje: error.message || 'Scanner desconectado.',
      tipoAlerta: 'SCANNER_DESCONECTADO',
      latenciaMs: 0,
      fechaScan: new Date().toISOString()
    });
  } finally {
    scanner.busy = false;
    scanButton.disabled = false;
    qrInput.focus();
  }
}

function renderResult(data) {
  const state = normalizeState(data.resultado);
  resultPanel.dataset.result = state;
  resultTitle.textContent = data.titulo || 'ACCESO DENEGADO';
  resultMessage.textContent = data.mensaje || data.tipoAlerta || 'Validación rechazada.';

  locationSection.textContent = data.seccion || 'PENDIENTE';
  locationRow.textContent = data.fila || 'PENDIENTE';
  locationSeat.textContent = data.asiento || 'PENDIENTE';
  latency.textContent = `${data.latenciaMs ?? 0} ms`;
  ticketId.textContent = `Ticket ${data.idTicket ?? '--'}`;
  scanId.textContent = `Registro ${data.idScan ?? '--'}`;

  if (state === 'valid') {
    setHealth('ready', 'Acceso autorizado');
  } else if (state === 'duplicate') {
    setHealth('error', 'QR YA ESCANEADO');
  } else {
    setHealth('error', data.tipoAlerta || 'Acceso denegado');
  }
}

function addHistory(data) {
  scanner.count += 1;
  scanCount.textContent = scanner.count.toString();

  const item = document.createElement('li');
  item.dataset.result = normalizeState(data.resultado);
  item.innerHTML = `
    <strong>${escapeHtml(data.titulo || data.resultado)}</strong>
    <span>${escapeHtml(data.mensaje || data.tipoAlerta || '')}</span>
    <small>${new Date(data.fechaScan).toLocaleTimeString()} · ${data.latenciaMs ?? 0} ms</small>
  `;

  scanHistory.prepend(item);

  while (scanHistory.children.length > 8) {
    scanHistory.lastElementChild.remove();
  }
}

function normalizeState(result) {
  const value = `${result || ''}`.toUpperCase();

  if (value === 'VALIDO') {
    return 'valid';
  }

  if (value === 'DUPLICADO') {
    return 'duplicate';
  }

  return value ? 'invalid' : 'idle';
}

function setHealth(state, text) {
  scanHealth.dataset.state = state;
  scanHealthText.textContent = text;
}

function resetInput() {
  scanner.buffer = '';
  qrInput.value = '';
}

function escapeHtml(value) {
  const element = document.createElement('span');
  element.textContent = value;
  return element.innerHTML;
}
