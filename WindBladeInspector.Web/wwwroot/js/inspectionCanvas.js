/**
 * QualiMax Blade Inspection Canvas
 * Vanilla JavaScript for handling canvas operations with zoom/pan/draw functionality.
 *
 * Key Features:
 * - Zoom via mouse wheel with CSS transform (zoom-to-cursor)
 * - Pan via middle-mouse or Alt+drag
 * - Inspection mode: draws red annotation quadrilaterals (4-point polygons snapped to rect)
 * - Click-to-select boxes with keyboard delete
 * - Right-click context menu for deletion
 * - Interop with Blazor via invokeMethodAsync
 */
window.inspectionCanvas = (function () {
    // ── State ─────────────────────────────────────────────────────────────────
    let canvas = null;
    let ctx = null;
    let dotNetRef = null;
    let containerEl = null;   // .canvas-container  (image + canvas overlay)
    let wrapperEl = null;     // .canvas-container-wrapper  (the scroll/clip zone)
    let imageEl = null;

    let mode = 'inspect';
    let isDrawing = false;
    let isPanning = false;

    let startX = 0, startY = 0;
    let currentX = 0, currentY = 0;
    let polygonPoints = [];
    let drawingPolygon = false;

    let scale = 1.0;
    let panX = 0, panY = 0;
    let lastPanX = 0, lastPanY = 0;
    let minScale = 0.1;

    let calibrationLine = null;
    let rootPoint = null;
    let anomalyBoxes = [];
    let selectedBoxIndex = -1;
    let hoveredBoxIndex = -1;

    // ── Init ──────────────────────────────────────────────────────────────────

    function initialize(canvasId, dotNetReference, imageId, containerId) {
        canvas = document.getElementById(canvasId);
        imageEl = document.getElementById(imageId);
        containerEl = document.getElementById(containerId);       // .canvas-container
        wrapperEl = containerEl ? containerEl.parentElement : null; // .canvas-container-wrapper
        dotNetRef = dotNetReference;

        if (!canvas || !imageEl || !containerEl) {
            console.error('[Canvas] Initialization failed – elements not found', { canvasId, imageId, containerId });
            return false;
        }

        ctx = canvas.getContext('2d');

        if (imageEl.complete && imageEl.naturalWidth > 0) {
            setupCanvas();
        } else {
            imageEl.onload = setupCanvas;
        }

        canvas.addEventListener('mousedown', onMouseDown);
        canvas.addEventListener('mousemove', onMouseMove);
        canvas.addEventListener('mouseup', onMouseUp);
        canvas.addEventListener('mouseleave', onMouseLeave);
        canvas.addEventListener('wheel', onWheel, { passive: false });
        canvas.addEventListener('contextmenu', onContextMenu);
        document.addEventListener('keydown', onKeyDown);

        // Re-fit when the browser window resizes
        window.addEventListener('resize', onWindowResize);

        console.log('[Canvas] Initialized with polygon + selection support');
        return true;
    }

    function setupCanvas() {
        if (!imageEl || !canvas) return;
        canvas.width = imageEl.naturalWidth;
        canvas.height = imageEl.naturalHeight;
        fitToContainer();
        minScale = scale;
        console.log(`[Canvas] Setup – image ${canvas.width}×${canvas.height}, fit scale ${minScale.toFixed(3)}`);
    }

    // ── Transform helpers ─────────────────────────────────────────────────────

    /** Fit the image inside the wrapper, centered. This is the canonical "reset". */
    function fitToContainer() {
        if (!canvas || !wrapperEl) return;

        const cw = wrapperEl.clientWidth  || wrapperEl.offsetWidth;
        const ch = wrapperEl.clientHeight || wrapperEl.offsetHeight;
        if (cw === 0 || ch === 0) return;

        const scaleX = cw / canvas.width;
        const scaleY = ch / canvas.height;
        scale = Math.min(scaleX, scaleY);           // contain — whole image visible

        // Center inside wrapper
        const scaledW = canvas.width  * scale;
        const scaledH = canvas.height * scale;
        panX = Math.round((cw - scaledW) / 2);
        panY = Math.round((ch - scaledH) / 2);

        applyTransform();
    }

    function applyTransform() {
        if (!containerEl) return;
        containerEl.style.transform       = `translate(${panX}px, ${panY}px) scale(${scale})`;
        containerEl.style.transformOrigin = '0 0';
        containerEl.style.position        = 'absolute';
        containerEl.style.top             = '0';
        containerEl.style.left            = '0';
    }

    function constrainPan() {
        if (!canvas || !wrapperEl) return;
        const cw = wrapperEl.clientWidth  || wrapperEl.offsetWidth;
        const ch = wrapperEl.clientHeight || wrapperEl.offsetHeight;
        const sw = canvas.width  * scale;
        const sh = canvas.height * scale;

        const maxOffX = sw * 0.8;
        const maxOffY = sh * 0.8;

        panX = Math.min(maxOffX, Math.max(cw - sw - maxOffX, panX));
        panY = Math.min(maxOffY, Math.max(ch - sh - maxOffY, panY));
    }

    function onWindowResize() {
        // Re-center without changing user zoom if they've manually zoomed
        if (scale === minScale) {
            fitToContainer();
        }
    }

    // ── Public zoom/pan API ───────────────────────────────────────────────────

    function setZoom(newScale) {
        if (!wrapperEl) return;
        const cw = wrapperEl.clientWidth  || wrapperEl.offsetWidth;
        const ch = wrapperEl.clientHeight || wrapperEl.offsetHeight;

        const prevScale = scale;
        scale = Math.max(0.1, Math.min(newScale, 10.0));

        // Zoom towards center of wrapper
        const factor = scale / prevScale;
        panX = cw / 2 - (cw / 2 - panX) * factor;
        panY = ch / 2 - (ch / 2 - panY) * factor;

        constrainPan();
        applyTransform();
    }

    function getZoom() { return scale; }

    function resetView() {
        fitToContainer();
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('ReceiveZoomChange', scale);
        }
    }

    // ── Mouse events ──────────────────────────────────────────────────────────

    function onWheel(e) {
        e.preventDefault();
        const delta = e.deltaY > 0 ? -0.1 : 0.1;
        const newScale = Math.max(minScale * 0.5, Math.min(scale + delta * scale, 10.0));
        if (newScale === scale) return;

        const rect = (wrapperEl || containerEl).getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        const factor = newScale / scale;
        panX = mouseX - (mouseX - panX) * factor;
        panY = mouseY - (mouseY - panY) * factor;

        scale = newScale;
        constrainPan();
        applyTransform();

        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('ReceiveZoomChange', scale);
        }
    }

    function onMouseDown(e) {
        e.preventDefault();
        const pos = getMousePos(e);

        // Middle-mouse or Alt+Left = pan
        if (e.button === 1 || (e.button === 0 && e.altKey)) {
            isPanning = true;
            lastPanX = e.clientX - panX;
            lastPanY = e.clientY - panY;
            canvas.style.cursor = 'grabbing';
            return;
        }

        if (mode === 'inspect' && e.button === 0) {
            // Click on existing box → select it
            if (!drawingPolygon && polygonPoints.length === 0) {
                const hit = findBoxAtPoint(pos);
                if (hit !== -1) {
                    selectedBoxIndex = hit;
                    redraw();
                    return;
                }
                selectedBoxIndex = -1;
            }

            // 4-point polygon drawing
            polygonPoints.push({ x: pos.x, y: pos.y });
            drawingPolygon = true;

            if (polygonPoints.length === 4) {
                handleInspectionEnd();
            } else {
                redraw();
                drawPolygonPreview();
            }
        } else if (mode === 'calibrate' && e.button === 0) {
            startX = pos.x; startY = pos.y;
            isDrawing = true;
            currentX = startX; currentY = startY;
        }
    }

    function onMouseMove(e) {
        if (isPanning) {
            panX = e.clientX - lastPanX;
            panY = e.clientY - lastPanY;
            constrainPan();
            applyTransform();
            return;
        }

        const pos = getMousePos(e);

        if (mode === 'inspect' && !drawingPolygon && polygonPoints.length === 0) {
            const newHover = findBoxAtPoint(pos);
            if (newHover !== hoveredBoxIndex) {
                hoveredBoxIndex = newHover;
                canvas.style.cursor = newHover !== -1 ? 'pointer' : 'crosshair';
                redraw();
            }
        }

        if (mode === 'calibrate' && isDrawing) {
            currentX = pos.x; currentY = pos.y;
            redraw(); drawPreview();
        } else if (mode === 'inspect' && drawingPolygon && polygonPoints.length > 0) {
            redraw();
            drawPolygonPreview(pos);
        }
    }

    function onMouseUp(e) {
        if (isPanning) {
            isPanning = false;
            updateCursor();
            return;
        }
        if (!isDrawing) return;
        isDrawing = false;
        if (mode === 'calibrate') {
            const pos = getMousePos(e);
            currentX = pos.x; currentY = pos.y;
            handleCalibrationEnd();
        }
    }

    function onMouseLeave(e) {
        if (isPanning) {
            isPanning = false;
            updateCursor();
        }
        hoveredBoxIndex = -1;
        redraw();
    }

    function onContextMenu(e) {
        e.preventDefault();
        const pos = getMousePos(e);
        const hit = findBoxAtPoint(pos);
        if (hit !== -1 && confirm(`Delete defect #${hit + 1}?`)) {
            deleteBoxByIndex(hit);
        }
        return false;
    }

    function onKeyDown(e) {
        if (e.key === 'Delete' || e.key === 'Backspace') {
            if (selectedBoxIndex !== -1) {
                e.preventDefault();
                deleteBoxByIndex(selectedBoxIndex);
            }
        } else if (e.key === 'Escape') {
            if (drawingPolygon) cancelCurrentDrawing();
            else if (selectedBoxIndex !== -1) { selectedBoxIndex = -1; redraw(); }
        }
    }

    // ── Coordinate helpers ────────────────────────────────────────────────────

    function getMousePos(e) {
        const rect = canvas.getBoundingClientRect();
        const sx = canvas.width  / rect.width;
        const sy = canvas.height / rect.height;
        return {
            x: (e.clientX - rect.left) * sx,
            y: (e.clientY - rect.top)  * sy
        };
    }

    function isPointInPolygon(pt, poly) {
        let inside = false;
        for (let i = 0, j = poly.length - 1; i < poly.length; j = i++) {
            const xi = poly[i].x, yi = poly[i].y;
            const xj = poly[j].x, yj = poly[j].y;
            const intersect = ((yi > pt.y) !== (yj > pt.y))
                && (pt.x < (xj - xi) * (pt.y - yi) / (yj - yi) + xi);
            if (intersect) inside = !inside;
        }
        return inside;
    }

    function findBoxAtPoint(pt) {
        for (let i = anomalyBoxes.length - 1; i >= 0; i--) {
            const box = anomalyBoxes[i];
            if (box.points && box.points.length === 4 && isPointInPolygon(pt, box.points)) return i;
        }
        return -1;
    }

    // ── Drawing logic ─────────────────────────────────────────────────────────

    function handleCalibrationEnd() {
        const dist = Math.hypot(currentX - startX, currentY - startY);
        if (dist > 10) {
            calibrationLine = { x1: startX, y1: startY, x2: currentX, y2: currentY };
            rootPoint = { x: startX, y: startY };
            redraw();
            if (dotNetRef) dotNetRef.invokeMethodAsync('ReceiveCalibration', dist, startX, startY, currentX, currentY);
        }
    }

    function handleInspectionEnd() {
        if (polygonPoints.length !== 4) return;

        const snapped = convertToRectangle(polygonPoints);
        const bounds  = getPolygonBounds(snapped);
        const area    = calculatePolygonArea(snapped);

        const newBox = { points: [...snapped], id: Date.now(), bounds, area };
        anomalyBoxes.push(newBox);
        redraw();

        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('ReceivePolygonSelection',
                snapped.map(p => p.x),
                snapped.map(p => p.y),
                area,
                bounds.width,
                bounds.height,
                canvas.width,
                canvas.height);
        }

        polygonPoints = [];
        drawingPolygon = false;
    }

    function convertToRectangle(points) {
        const cx = points.reduce((s, p) => s + p.x, 0) / 4;
        const cy = points.reduce((s, p) => s + p.y, 0) / 4;

        const sorted = points.slice().sort((a, b) =>
            Math.atan2(a.y - cy, a.x - cx) - Math.atan2(b.y - cy, b.x - cx));

        let maxDist = 0, p1 = 0, p2 = 1;
        for (let i = 0; i < 4; i++) {
            const j = (i + 1) % 4;
            const d = Math.hypot(sorted[j].x - sorted[i].x, sorted[j].y - sorted[i].y);
            if (d > maxDist) { maxDist = d; p1 = i; p2 = j; }
        }

        const angle = Math.atan2(sorted[p2].y - sorted[p1].y, sorted[p2].x - sorted[p1].x);
        const cosA = Math.cos(angle), sinA = Math.sin(angle);

        const proj = sorted.map(p => ({
            u:  (p.x - cx) * cosA + (p.y - cy) * sinA,
            v: -(p.x - cx) * sinA + (p.y - cy) * cosA
        }));

        const minU = Math.min(...proj.map(p => p.u)), maxU = Math.max(...proj.map(p => p.u));
        const minV = Math.min(...proj.map(p => p.v)), maxV = Math.max(...proj.map(p => p.v));

        return [
            { u: minU, v: minV }, { u: maxU, v: minV },
            { u: maxU, v: maxV }, { u: minU, v: maxV }
        ].map(c => ({
            x: cx + c.u * cosA - c.v * sinA,
            y: cy + c.u * sinA + c.v * cosA
        }));
    }

    function calculatePolygonArea(pts) {
        let a = 0;
        for (let i = 0; i < pts.length; i++) {
            const j = (i + 1) % pts.length;
            a += pts[i].x * pts[j].y - pts[j].x * pts[i].y;
        }
        return Math.abs(a / 2);
    }

    function getPolygonBounds(pts) {
        const xs = pts.map(p => p.x), ys = pts.map(p => p.y);
        const minX = Math.min(...xs), maxX = Math.max(...xs);
        const minY = Math.min(...ys), maxY = Math.max(...ys);
        return { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
    }

    // ── Render ────────────────────────────────────────────────────────────────

    function redraw() {
        if (!ctx) return;
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        anomalyBoxes.forEach((box, idx) => {
            if (!box.points || box.points.length !== 4) return;

            const isSel     = idx === selectedBoxIndex;
            const isHovered = idx === hoveredBoxIndex;
            const stroke    = isSel ? '#ffff00' : isHovered ? '#ff8800' : '#ff3333';
            const fill      = isSel ? 'rgba(255,255,0,0.2)' : isHovered ? 'rgba(255,136,0,0.15)' : 'rgba(255,51,51,0.15)';
            const lw        = isSel ? 4 : isHovered ? 3.5 : 2.5;

            drawPolygon(box.points, stroke, fill, lw);

            // Label
            ctx.fillStyle = stroke;
            ctx.font = 'bold 16px Inter, sans-serif';
            ctx.fillText(`#${idx + 1}`, box.points[0].x + 8, box.points[0].y + 22);

            if (isSel || isHovered) drawDeleteButton(box.points[0].x, box.points[0].y, idx);
        });
    }

    function drawPolygon(pts, strokeColor, fillColor, lw) {
        if (pts.length < 3) return;
        ctx.beginPath();
        ctx.moveTo(pts[0].x, pts[0].y);
        for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i].x, pts[i].y);
        ctx.closePath();
        if (fillColor)   { ctx.fillStyle = fillColor; ctx.fill(); }
        if (strokeColor) { ctx.strokeStyle = strokeColor; ctx.lineWidth = lw || 2; ctx.stroke(); }
    }

    function drawDeleteButton(x, y, index) {
        const bx = x + 50, by = y - 10, r = 12;
        ctx.beginPath();
        ctx.arc(bx, by, r, 0, Math.PI * 2);
        ctx.fillStyle = '#ff2222'; ctx.fill();
        ctx.strokeStyle = '#fff'; ctx.lineWidth = 2; ctx.stroke();
        ctx.strokeStyle = '#fff'; ctx.lineWidth = 2.5;
        const o = 5;
        ctx.beginPath();
        ctx.moveTo(bx - o, by - o); ctx.lineTo(bx + o, by + o);
        ctx.moveTo(bx + o, by - o); ctx.lineTo(bx - o, by + o);
        ctx.stroke();
    }

    function drawPreview() {
        if (mode !== 'calibrate') return;
        ctx.beginPath();
        ctx.moveTo(startX, startY); ctx.lineTo(currentX, currentY);
        ctx.strokeStyle = '#00bfff'; ctx.lineWidth = 3;
        ctx.setLineDash([10, 5]); ctx.stroke(); ctx.setLineDash([]);
    }

    function drawPolygonPreview(cursorPos) {
        if (polygonPoints.length === 0) return;
        polygonPoints.forEach((pt, idx) => {
            ctx.beginPath();
            ctx.arc(pt.x, pt.y, 5, 0, Math.PI * 2);
            ctx.fillStyle = '#ff3333'; ctx.fill();
            ctx.strokeStyle = '#fff'; ctx.lineWidth = 2; ctx.stroke();
            ctx.fillStyle = '#ff3333'; ctx.font = 'bold 14px Inter, sans-serif';
            ctx.fillText(`${idx + 1}`, pt.x + 10, pt.y - 10);
        });

        if (polygonPoints.length > 1) {
            ctx.beginPath();
            ctx.moveTo(polygonPoints[0].x, polygonPoints[0].y);
            for (let i = 1; i < polygonPoints.length; i++) ctx.lineTo(polygonPoints[i].x, polygonPoints[i].y);
            ctx.strokeStyle = '#ff3333'; ctx.lineWidth = 2;
            ctx.setLineDash([8, 4]); ctx.stroke(); ctx.setLineDash([]);
        }

        if (cursorPos && polygonPoints.length < 4) {
            const last = polygonPoints[polygonPoints.length - 1];
            ctx.beginPath();
            ctx.moveTo(last.x, last.y); ctx.lineTo(cursorPos.x, cursorPos.y);
            ctx.strokeStyle = '#ff9999'; ctx.lineWidth = 1;
            ctx.setLineDash([4, 4]); ctx.stroke(); ctx.setLineDash([]);
        }

        if (polygonPoints.length === 3 && cursorPos) {
            ctx.beginPath();
            ctx.moveTo(cursorPos.x, cursorPos.y);
            ctx.lineTo(polygonPoints[0].x, polygonPoints[0].y);
            ctx.strokeStyle = '#ffbbbb'; ctx.lineWidth = 1;
            ctx.setLineDash([2, 4]); ctx.stroke(); ctx.setLineDash([]);
        }
    }

    // ── Box management ────────────────────────────────────────────────────────

    function deleteBoxByIndex(index) {
        if (index < 0 || index >= anomalyBoxes.length) return false;
        anomalyBoxes.splice(index, 1);
        if (selectedBoxIndex === index) selectedBoxIndex = -1;
        else if (selectedBoxIndex > index) selectedBoxIndex--;
        redraw();
        if (dotNetRef) dotNetRef.invokeMethodAsync('ReceiveBoxDeleted', index);
        return true;
    }

    function removeLastBox() {
        if (anomalyBoxes.length > 0) { anomalyBoxes.pop(); selectedBoxIndex = -1; redraw(); }
    }

    function cancelCurrentDrawing() {
        polygonPoints = []; drawingPolygon = false; redraw();
    }

    function clearAll() {
        calibrationLine = null; rootPoint = null;
        anomalyBoxes = []; polygonPoints = []; drawingPolygon = false;
        selectedBoxIndex = -1; hoveredBoxIndex = -1;
        redraw();
    }

    function loadAnomalies(boxes) {
        anomalyBoxes = boxes || [];
        selectedBoxIndex = -1;
        redraw();
    }

    function removeBoxByIndex(index) { return deleteBoxByIndex(index); }
    function undoLastBox()           { return removeLastBox(); }
    function clearLastDrawnBox()     { return removeLastBox(); }
    function getAnomalyCount()       { return anomalyBoxes.length; }
    function setMode(newMode)        { mode = newMode; selectedBoxIndex = -1; updateCursor(); redraw(); }
    function updateCursor()          { canvas.style.cursor = mode === 'inspect' ? 'crosshair' : mode === 'pan' ? 'grab' : 'default'; }

    // ── Public API ────────────────────────────────────────────────────────────
    return {
        initialize,
        setMode,
        setZoom,
        getZoom,
        resetView,
        clearAll,
        undoLastBox,
        loadAnomalies,
        getAnomalyCount,
        removeBoxByIndex,
        clearLastDrawnBox,
        cancelCurrentDrawing,
        deleteBoxByIndex
    };
})();

window.constWindowOpen = function (htmlContent) {
    const win = window.open('', '_blank');
    if (win) { win.document.write(htmlContent); win.document.close(); }
    else      { alert('Please allow popups to view the report.'); }
};