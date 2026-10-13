// Antes de pintar: el tema (claro u oscuro) y el panel de filtros plegado o abierto, para que no parpadee.
// Las elecciones se guardan en el navegador con las claves «ygmm-tema» y «ygmm-panel».
(function () {
    var raiz = document.documentElement;
    var modo = 'auto';
    var panel = '';
    try {
        modo = localStorage.getItem('ygmm-tema') || 'auto';
        panel = localStorage.getItem('ygmm-panel') || '';
    } catch (e) { /* sin almacenamiento */ }
    var oscuro = modo === 'oscuro' ||
        (modo === 'auto' && window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
    raiz.setAttribute('data-tema', oscuro ? 'oscuro' : 'claro');
    raiz.setAttribute('data-modo-tema', modo);
    if (panel === 'plegado') raiz.setAttribute('data-panel', 'plegado');
})();
