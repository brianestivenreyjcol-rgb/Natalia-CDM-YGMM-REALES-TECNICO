// Cuadro de mando YGMM Técnico, al estilo del portal SOLARIS: tema, panel de filtros plegable, filtros con
// recarga parcial, imprimir, presentar, fichas de las gráficas, tablas que se ordenan con semáforo y barrita
// de volumen. Sin librerías externas.
(function () {
    'use strict';

    const raiz = document.documentElement;
    const CLAVE_TEMA = 'ygmm-tema';
    const CLAVE_PANEL = 'ygmm-panel';
    const MODOS = ['auto', 'claro', 'oscuro'];
    const TEXTOS_TEMA = { auto: 'Tema: automático', claro: 'Tema: claro', oscuro: 'Tema: oscuro' };
    const oscuroDelSistema = window.matchMedia('(prefers-color-scheme: dark)');

    const leer = clave => { try { return localStorage.getItem(clave); } catch { return null; } };
    const guardar = (clave, valor) => { try { localStorage.setItem(clave, valor); } catch { /* sin almacenamiento */ } };

    // ───────── Tema ─────────

    const leerModo = () => leer(CLAVE_TEMA) || 'auto';

    function aplicarTema(modo) {
        const oscuro = modo === 'oscuro' || (modo === 'auto' && oscuroDelSistema.matches);
        raiz.dataset.tema = oscuro ? 'oscuro' : 'claro';
        raiz.dataset.modoTema = modo;
        document.querySelectorAll('[data-texto-tema]').forEach(el => { el.textContent = TEXTOS_TEMA[modo]; });
    }

    function cambiarTema() {
        const modo = MODOS[(MODOS.indexOf(leerModo()) + 1) % MODOS.length];
        guardar(CLAVE_TEMA, modo);
        aplicarTema(modo);
    }

    oscuroDelSistema.addEventListener('change', () => aplicarTema(leerModo()));

    // ───────── Panel de filtros plegable ─────────

    function plegarPanel(plegado) {
        if (plegado) raiz.dataset.panel = 'plegado'; else delete raiz.dataset.panel;
        guardar(CLAVE_PANEL, plegado ? 'plegado' : 'abierto');
    }

    // ───────── Presentar: pantalla completa, sin panel ni botones ─────────

    function presentar(activo) {
        const salir = document.querySelector('[data-presentar-salir]');
        if (activo) {
            raiz.dataset.presentacion = '';
            if (salir) salir.hidden = false;
            document.documentElement.requestFullscreen?.().catch(() => { });
        } else {
            delete raiz.dataset.presentacion;
            if (salir) salir.hidden = true;
            if (document.fullscreenElement) document.exitFullscreen?.().catch(() => { });
        }
    }

    document.addEventListener('fullscreenchange', () => {
        if (!document.fullscreenElement && 'presentacion' in raiz.dataset) presentar(false);
    });

    // Al imprimir, siempre en claro
    let temaAntesDeImprimir = null;
    window.addEventListener('beforeprint', () => { temaAntesDeImprimir = raiz.dataset.tema; raiz.dataset.tema = 'claro'; });
    window.addEventListener('afterprint', () => { if (temaAntesDeImprimir) raiz.dataset.tema = temaAntesDeImprimir; });

    // ───────── Barra de carga ─────────

    const barra = document.getElementById('barra-carga');

    function cargando(activo) {
        if (!barra) return;
        if (activo) {
            barra.classList.remove('fin');
            barra.classList.add('activa');
        } else if (barra.classList.contains('activa')) {
            barra.classList.remove('activa');
            barra.classList.add('fin');
            setTimeout(() => barra.classList.remove('fin'), 400);
        }
    }

    window.addEventListener('pageshow', () => cargando(false));

    // ───────── Filtros y recarga parcial ─────────

    let ultimaPeticion = 0;

    /** Recuerda el desplegable abierto (búsqueda, desplazamiento y foco) para dejarlo igual tras recargar. */
    function recordarDesplegable() {
        const abierto = document.querySelector('details.multi[open]');
        if (!abierto) return null;
        const foco = abierto.contains(document.activeElement) ? document.activeElement : null;
        return {
            campo: abierto.dataset.campo,
            busqueda: abierto.querySelector('[data-buscar]')?.value || '',
            desplazamiento: abierto.querySelector('.multi-lista')?.scrollTop || 0,
            foco: foco?.matches('input[type="checkbox"]') ? foco.value : (foco?.matches('[data-buscar]') ? 'buscar' : null),
        };
    }

    function reabrirDesplegable(recuerdo) {
        if (!recuerdo) return;
        const desplegable = [...document.querySelectorAll('details.multi')].find(d => d.dataset.campo === recuerdo.campo);
        if (!desplegable) return;
        desplegable.open = true;
        const buscar = desplegable.querySelector('[data-buscar]');
        if (buscar) { buscar.value = recuerdo.busqueda; filtrarOpciones(buscar); }
        const lista = desplegable.querySelector('.multi-lista');
        if (lista) lista.scrollTop = recuerdo.desplazamiento;
        if (recuerdo.foco === 'buscar' && buscar) buscar.focus({ preventScroll: true });
        else if (recuerdo.foco) {
            [...desplegable.querySelectorAll('input[type="checkbox"]')].find(c => c.value === recuerdo.foco)?.focus({ preventScroll: true });
        }
    }

    /** Trae la página con los filtros nuevos y cambia solo #informe, sin repetir las animaciones de entrada. */
    async function recargar(url, apilar = true) {
        const peticion = ++ultimaPeticion;
        const recuerdo = recordarDesplegable();
        cargando(true);
        try {
            const respuesta = await fetch(url, { headers: { 'X-Requested-With': 'fetch' } });
            if (!respuesta.ok) throw new Error(String(respuesta.status));
            const html = await respuesta.text();
            if (peticion !== ultimaPeticion) return;
            const documento = new DOMParser().parseFromString(html, 'text/html');
            const nuevo = documento.getElementById('informe');
            const actual = document.getElementById('informe');
            if (!nuevo || !actual) { location.href = url; return; }
            nuevo.classList.add('actualizado');
            actual.replaceWith(nuevo);
            document.title = documento.title;
            if (apilar) history.pushState(null, '', url);
            aplicarTema(leerModo());
            if ('presentacion' in raiz.dataset) { const salir = nuevo.querySelector('[data-presentar-salir]'); if (salir) salir.hidden = false; }
            prepararTablas(nuevo);
            reabrirDesplegable(recuerdo);
            programarRecarga();
        } catch {
            location.href = url;
        } finally {
            if (peticion === ultimaPeticion) cargando(false);
        }
    }

    /** URL con lo marcado en el formulario; las fechas por defecto no se escriben. */
    function urlDeFiltros(formulario) {
        const datos = new URLSearchParams();
        for (const [clave, valor] of new FormData(formulario)) {
            if (valor !== '') datos.append(clave, valor);
        }
        const desde = formulario.querySelector('[name="desde"]');
        const hasta = formulario.querySelector('[name="hasta"]');
        if (desde && hasta && desde.value === desde.dataset.defecto && hasta.value === hasta.dataset.defecto) {
            datos.delete('desde');
            datos.delete('hasta');
        }
        const consulta = datos.toString();
        return formulario.getAttribute('action') + (consulta ? '?' + consulta : '');
    }

    function cerrarDesplegables(excepto) {
        document.querySelectorAll('details.multi[open]').forEach(d => { if (d !== excepto) d.open = false; });
    }

    const normalizar = texto => texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();

    function filtrarOpciones(buscador) {
        const texto = normalizar(buscador.value);
        buscador.closest('.multi-panel').querySelectorAll('label.opcion').forEach(opcion => {
            opcion.hidden = texto !== '' && !normalizar(opcion.textContent).includes(texto);
        });
    }

    // Cada casilla y cada fecha filtran al momento (salvo en [data-sin-auto], que espera a su botón: p. ej. la clave de duplicados)
    document.addEventListener('change', e => {
        const campo = e.target.closest('form[data-filtros] :is(input[type="checkbox"], input[type="date"])');
        if (!campo || campo.closest('[data-sin-auto]')) return;
        if (campo.type === 'date' && !campo.value) return;
        recargar(urlDeFiltros(campo.form));
    });

    document.addEventListener('submit', e => {
        const formulario = e.target.closest('form[data-filtros]');
        if (!formulario) return;
        e.preventDefault();
        cerrarDesplegables();
        recargar(urlDeFiltros(formulario));
    });

    document.addEventListener('input', e => {
        if (e.target.matches('[data-buscar]')) filtrarOpciones(e.target);
    });

    // Solo un desplegable abierto a la vez; al abrir, el buscador recibe el foco
    document.addEventListener('toggle', e => {
        const desplegable = e.target;
        if (!(desplegable instanceof HTMLDetailsElement) || !desplegable.open || !desplegable.matches('.multi')) return;
        cerrarDesplegables(desplegable);
        desplegable.querySelector('[data-buscar]')?.focus({ preventScroll: true });
    }, true);

    document.addEventListener('keydown', e => {
        if (e.key !== 'Escape') return;
        const abierto = document.querySelector('details.multi[open]');
        if (abierto) { abierto.open = false; abierto.querySelector('summary')?.focus(); return; }
        if ('presentacion' in raiz.dataset) presentar(false);
    });

    window.addEventListener('popstate', () => recargar(location.href, false));

    // ───────── Clics ─────────

    document.addEventListener('click', e => {
        if (!e.target.closest('details.multi')) cerrarDesplegables();

        if (e.target.closest('[data-boton-tema]')) { cambiarTema(); return; }
        if (e.target.closest('[data-panel-plegar]')) { plegarPanel(true); return; }
        if (e.target.closest('[data-panel-abrir]')) { plegarPanel(false); return; }
        if (e.target.closest('[data-imprimir]')) { window.print(); return; }
        if (e.target.closest('[data-presentar]')) { presentar(true); return; }
        if (e.target.closest('[data-presentar-salir]')) { presentar(false); return; }

        const quitar = e.target.closest('[data-quitar]');
        if (quitar) {
            quitar.closest('details').querySelectorAll('input[type="checkbox"]').forEach(c => { c.checked = false; });
            recargar(urlDeFiltros(quitar.form));
            return;
        }

        const ordenar = e.target.closest('button.ordenar');
        if (ordenar) { ordenarTabla(ordenar); return; }

        const enlace = e.target.closest('a[href]');
        if (!enlace || e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;
        if (enlace.hasAttribute('data-parcial')) {
            e.preventDefault();
            cerrarDesplegables();
            recargar(enlace.href);
        } else if (enlace.origin === location.origin && !enlace.classList.contains('enlace-exportable') && !enlace.hasAttribute('download')) {
            cargando(true);
        }
    });

    // ───────── Tablas: orden, semáforo y barrita de volumen ─────────

    const valorDe = celda => {
        const crudo = celda?.dataset.valor;
        return crudo === undefined || crudo === '' ? null : parseFloat(crudo);
    };

    function ordenarTabla(boton) {
        const cabecera = boton.closest('th');
        const tabla = cabecera.closest('table');
        const cuerpo = tabla.tBodies[0];
        const columna = [...cabecera.parentElement.cells].indexOf(cabecera);
        const ascendente = cabecera.getAttribute('aria-sort') !== 'ascending';
        cabecera.parentElement.querySelectorAll('th').forEach(th => th.removeAttribute('aria-sort'));
        cabecera.setAttribute('aria-sort', ascendente ? 'ascending' : 'descending');

        const clave = fila => {
            const celda = fila.cells[columna];
            if (!celda) return '';
            if (celda.hasAttribute('data-valor')) return valorDe(celda) ?? Number.NEGATIVE_INFINITY;
            return celda.textContent.trim();
        };
        const filas = [...cuerpo.rows].sort((a, b) => {
            const x = clave(a), y = clave(b);
            const r = typeof x === 'number' && typeof y === 'number'
                ? x - y
                : String(x).localeCompare(String(y), 'es', { numeric: true });
            return ascendente ? r : -r;
        });
        cuerpo.append(...filas);
    }

    /** Semáforo pastel por columna (th[data-sentido]: 1 más es mejor, −1 más es peor, 0 sin color) y --v de las barritas. */
    function prepararTablas(raizNodo) {
        raizNodo.querySelectorAll('table[data-mapa]').forEach(tabla => {
            const cabeceras = [...(tabla.tHead?.rows[0]?.cells ?? [])];
            const filas = [...tabla.tBodies[0].rows];
            cabeceras.forEach((th, i) => {
                const celdas = filas.map(f => f.cells[i]).filter(Boolean);
                const valores = celdas.map(valorDe).filter(v => v !== null);
                if (valores.length < 2) return;
                const min = Math.min(...valores), max = Math.max(...valores);

                celdas.filter(td => td.classList.contains('barra-volumen')).forEach(td => {
                    const v = valorDe(td);
                    td.style.setProperty('--v', v === null || max <= 0 ? '0' : (v / max).toFixed(3));
                });

                const sentido = Number(th.dataset.sentido || 0);
                if (!sentido || !(max > min)) return;
                celdas.forEach(td => {
                    const v = valorDe(td);
                    td.classList.remove('mapa-verde', 'mapa-ambar', 'mapa-rojo');
                    if (v === null) return;
                    let t = (v - min) / (max - min);
                    if (sentido < 0) t = 1 - t;
                    td.classList.add(t >= 2 / 3 ? 'mapa-verde' : t <= 1 / 3 ? 'mapa-rojo' : 'mapa-ambar');
                });
            });
        });
    }

    // ───────── Ficha al pasar el ratón ─────────
    // data-ficha="Cabecera|serie-x~Nombre · valor|…": cada línea con el cuadradito del color de su serie.

    const ficha = document.getElementById('ficha');
    let elementoFicha = null;

    function pintarFicha(texto) {
        const [cabecera, ...lineas] = texto.split('|');
        const titulo = document.createElement('strong');
        titulo.textContent = cabecera;
        ficha.replaceChildren(titulo);
        for (const linea of lineas) {
            const fila = document.createElement('span');
            fila.className = 'ficha-linea';
            const separador = linea.indexOf('~');
            const punto = document.createElement('i');
            if (separador > 0) punto.className = linea.slice(0, separador);
            fila.append(punto, separador > 0 ? linea.slice(separador + 1) : linea);
            ficha.append(fila);
        }
    }

    function colocarFicha(x, y) {
        const margen = 14;
        const ancho = ficha.offsetWidth, alto = ficha.offsetHeight;
        let izquierda = x + margen, arriba = y + margen;
        if (izquierda + ancho > window.innerWidth - 8) izquierda = x - ancho - margen;
        if (arriba + alto > window.innerHeight - 8) arriba = y - alto - margen;
        ficha.style.left = Math.max(8, izquierda) + 'px';
        ficha.style.top = Math.max(8, arriba) + 'px';
    }

    function ocultarFicha() {
        if (!ficha || ficha.hidden) return;
        ficha.hidden = true;
        elementoFicha = null;
    }

    if (ficha) {
        document.addEventListener('pointermove', e => {
            const elemento = e.target instanceof Element ? e.target.closest('[data-ficha]') : null;
            if (!elemento) { ocultarFicha(); return; }
            if (elemento !== elementoFicha) {
                elementoFicha = elemento;
                pintarFicha(elemento.dataset.ficha);
                ficha.hidden = false;
            }
            colocarFicha(e.clientX, e.clientY);
        });
        document.addEventListener('pointerleave', ocultarFicha);
        window.addEventListener('scroll', ocultarFicha, { passive: true });
    }

    // ───────── Páginas que se recargan solas (preparando datos, estado) ─────────

    let temporizador = null;

    function programarRecarga() {
        clearTimeout(temporizador);
        // Razor escribe siempre los atributos data-* (vacíos si su valor es null): vacío = no recargar
        const elemento = document.querySelector('[data-recargar-en]:not([data-recargar-en=""])');
        if (!elemento) return;
        const segundos = Number(elemento.dataset.recargarEn) || 10;
        temporizador = setTimeout(() => location.reload(), segundos * 1000);
    }

    aplicarTema(leerModo());
    prepararTablas(document);
    programarRecarga();
})();
