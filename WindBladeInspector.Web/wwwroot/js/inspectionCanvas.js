/**
 * AeroEdge Blade Inspection Canvas
 * Vanilla JavaScript for handling canvas operations with zoom/pan/draw functionality.
 * 
 * Key Features:
 * - Zoom via mouse wheel with CSS transform
 * - Pan via click-and-drag
 * - Calibration mode: draws blue line (Root → Tip)
 * - Inspection mode: draws red annotation boxes
 * - Interop with Blazor via invokeMethodAsync
 */
window.inspectionCanvas = (function () {
    // State
    let canvas = null;
    let ctx = null;
    let dotNetRef = null;
    let containerEl = null;
    let imageEl = null;

    // Modes
    let mode = 'calibrate'; // 'calibrate', 'inspect', 'pan'
    let isDrawing = false;
    let isPanning = false;

    // Coordinates
    let startX = 0, startY = 0;
    let currentX = 0, currentY = 0;

    // Pan/Zoom state
    let scale = 1.0;
    let panX = 0, panY = 0;
    let lastPanX = 0, lastPanY = 0;

    // Stored drawings
    let calibrationLine = null;
    let rootPoint = null;
    let anomalyBoxes = [];

    /**
     * Initialize the canvas with element IDs and .NET reference.
     */
    function initialize(canvasId, dotNetReference, imageId, containerId) {
        canvas = document.getElementById(canvasId);
        imageEl = document.getElementById(imageId);
        containerEl = document.getElementById(containerId);
        dotNetRef = dotNetReference;

        if (!canvas || !imageEl || !containerEl) {
            console.error('Canvas initialization failed - elements not found');
            return false;
        }

        ctx = canvas.getContext('2d');

        // Wait for image to load
        if (imageEl.complete) {
            setupCanvas();
        } else {
            imageEl.onload = setupCanvas;
        }

        // Attach event listeners
        canvas.addEventListener('mousedown', onMouseDown);
        canvas.addEventListener('mousemove', onMouseMove);
        canvas.addEventListener('mouseup', onMouseUp);
        canvas.addEventListener('mouseleave', onMouseUp);
        canvas.addEventListener('wheel', onWheel, { passive: false });

        // Context menu prevention
        canvas.addEventListener('contextmenu', (e) => e.preventDefault());

        console.log('AeroEdge Canvas initialized');
        return true;
    }

    function setupCanvas() {
        canvas.width = imageEl.naturalWidth;
        canvas.height = imageEl.naturalHeight;
        resetView(); // Use shared logic for initial fit
    }

    /**
     * Set the interaction mode.
     */
    function setMode(newMode) {
        mode = newMode;
        updateCursor();
    }

    function updateCursor() {
        switch (mode) {
            case 'inspect':
                canvas.style.cursor = 'crosshair';
                break;
            case 'pan':
                canvas.style.cursor = isPanning ? 'grabbing' : 'grab';
                break;
            default:
                canvas.style.cursor = 'default';
        }
    }

    /**
     * Zoom handler via mouse wheel.
     */
    function onWheel(e) {
        e.preventDefault();
        const delta = e.deltaY > 0 ? -0.1 : 0.1;
        const newScale = Math.max(0.25, Math.min(scale + delta, 10.0)); // Increase max zoom

        // Zoom towards cursor position
        const rect = containerEl.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        // Adjust pan to zoom towards mouse
        const scaleFactor = newScale / scale;
        panX = mouseX - (mouseX - panX) * scaleFactor;
        panY = mouseY - (mouseY - panY) * scaleFactor;

        scale = newScale;
        applyTransform();

        // Notify C# of zoom change
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('ReceiveZoomChange', scale);
        }
    }

    function applyTransform() {
        if (containerEl) {
            containerEl.style.transform = `translate(${panX}px, ${panY}px) scale(${scale})`;
            containerEl.style.transformOrigin = '0 0';
        }
    }

    /**
     * Set zoom level programmatically.
     */
    function setZoom(newScale) {
        scale = Math.max(0.25, Math.min(newScale, 5.0));
        applyTransform();
    }

    function getZoom() {
        return scale;
    }

    /**
     * Reset view to default (Center and Fit).
     */
    function resetView() {
        if (!canvas || !containerEl) return;

        // Get container dimensions
        const containerRect = containerEl.getBoundingClientRect();
        const containerW = containerRect.width;
        const containerH = containerRect.height;

        // Get image dimensions
        const imgW = canvas.width;
        const imgH = canvas.height;

        if (containerW === 0 || containerH === 0) return; // Container not visible yet

        // Calculate scale to fit image entirely within container (contain)
        const scaleX = containerW / imgW;
        const scaleY = containerH / imgH;
        // Use the smaller scale so the whole image fits
        let fitScale = Math.min(scaleX, scaleY);
        
        // Cap the max initial scale to 1.0 (actual size) if image is smaller than screen
        // or remove this line if you want small images to stretch
        fitScale = Math.min(fitScale, 1.0) * 0.9; // 0.9 provides a small padding margin

        scale = fitScale;

        // Calculate centered position
        // The transform origin is 0 0, so we calculate the top-left coordinate that centers the scaled image
        const scaledW = imgW * scale;
        const scaledH = imgH * scale;

        panX = (containerW - scaledW) / 2;
        panY = (containerH - scaledH) / 2;

        applyTransform();
    }

    /**
     * Get mouse position relative to the canvas, accounting for transforms.
     */
    function getMousePos(e) {
        const rect = canvas.getBoundingClientRect();
        const scaleX = canvas.width / rect.width;
        const scaleY = canvas.height / rect.height;
        return {
            x: (e.clientX - rect.left) * scaleX,
            y: (e.clientY - rect.top) * scaleY
        };
    }

    function onMouseDown(e) {
        e.preventDefault();
        const pos = getMousePos(e);
        startX = pos.x;
        startY = pos.y;

        if (mode === 'pan' || e.button === 1 || (e.button === 0 && e.altKey)) {
            // Middle mouse or Alt+Click = Pan
            isPanning = true;
            lastPanX = e.clientX - panX;
            lastPanY = e.clientY - panY;
            canvas.style.cursor = 'grabbing';
        } else {
            isDrawing = true;
            currentX = startX;
            currentY = startY;
        }
    }

    function onMouseMove(e) {
        if (isPanning) {
            panX = e.clientX - lastPanX;
            panY = e.clientY - lastPanY;
            applyTransform();
            return;
        }

        if (!isDrawing) return;

        const pos = getMousePos(e);
        currentX = pos.x;
        currentY = pos.y;

        redraw();
        drawPreview();
    }

    function onMouseUp(e) {
        if (isPanning) {
            isPanning = false;
            updateCursor();
            return;
        }

        if (!isDrawing) return;
        isDrawing = false;

        const pos = getMousePos(e);
        currentX = pos.x;
        currentY = pos.y;

        if (mode === 'calibrate') {
            handleCalibrationEnd();
        } else if (mode === 'inspect') {
            handleInspectionEnd();
        }
    }

    function handleCalibrationEnd() {
        const dist = Math.sqrt(
            Math.pow(currentX - startX, 2) +
            Math.pow(currentY - startY, 2)
        );

        if (dist > 10) {
            calibrationLine = {
                x1: startX, y1: startY,
                x2: currentX, y2: currentY
            };
            rootPoint = { x: startX, y: startY };

            redraw();

            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('ReceiveCalibration', dist, startX, startY, currentX, currentY);
            }
        }
    }

    function handleInspectionEnd() {
        const x = Math.min(startX, currentX);
        const y = Math.min(startY, currentY);
        const w = Math.abs(currentX - startX);
        const h = Math.abs(currentY - startY);

        if (w > 10 && h > 10) {
            const newBox = { x, y, w, h, id: Date.now() };
            anomalyBoxes.push(newBox);

            redraw();

            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('ReceiveSelection', x, y, w, h, canvas.width, canvas.height);
            }
        }
    }

    /**
     * Remove the last added anomaly box (for Undo or Cancel).
     */
    function removeLastBox() {
        if (anomalyBoxes.length > 0) {
            anomalyBoxes.pop();
            redraw();
        }
    }

    /**
     * Redraw all stored annotations.
     */
    function redraw() {
        if (!ctx) return;
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        // Draw anomaly boxes (red)
        anomalyBoxes.forEach((box, idx) => {
            ctx.fillStyle = 'rgba(255, 0, 0, 0.2)';
            ctx.fillRect(box.x, box.y, box.w, box.h);
            ctx.strokeStyle = '#ff0000';
            ctx.lineWidth = 3;
            ctx.strokeRect(box.x, box.y, box.w, box.h);

            // Label
            ctx.fillStyle = '#ff0000';
            ctx.font = 'bold 16px Inter, sans-serif';
            ctx.fillText(`#${idx + 1}`, box.x + 8, box.y + 22);
        });
    }

    function drawCircle(x, y, radius, color, label) {
        ctx.beginPath();
        ctx.arc(x, y, radius, 0, Math.PI * 2);
        ctx.fillStyle = color;
        ctx.fill();
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 2;
        ctx.stroke();

        if (label) {
            ctx.fillStyle = '#fff';
            ctx.font = 'bold 12px Inter, sans-serif';
            ctx.fillText(label, x + 12, y + 4);
        }
    }

    function drawPreview() {
        if (mode === 'calibrate') {
            // Blue dashed line preview
            ctx.beginPath();
            ctx.moveTo(startX, startY);
            ctx.lineTo(currentX, currentY);
            ctx.strokeStyle = '#00bfff';
            ctx.lineWidth = 3;
            ctx.setLineDash([10, 5]);
            ctx.stroke();
            ctx.setLineDash([]);
        } else if (mode === 'inspect') {
            // Red box preview
            const x = Math.min(startX, currentX);
            const y = Math.min(startY, currentY);
            const w = Math.abs(currentX - startX);
            const h = Math.abs(currentY - startY);

            ctx.fillStyle = 'rgba(255, 0, 0, 0.15)';
            ctx.fillRect(x, y, w, h);
            ctx.strokeStyle = '#ff0000';
            ctx.lineWidth = 2;
            ctx.setLineDash([8, 4]);
            ctx.strokeRect(x, y, w, h);
            ctx.setLineDash([]);
        }
    }

    /**
     * Clear all annotations.
     */
    function clearAll() {
        calibrationLine = null;
        rootPoint = null;
        anomalyBoxes = [];
        redraw();
    }

    /**
     * Remove the last anomaly box.
     */
    function undoLastBox() {
        if (anomalyBoxes.length > 0) {
            anomalyBoxes.pop();
            redraw();
            return true;
        }
        return false;
    }

    /**
     * Load existing anomaly boxes (e.g., from saved data).
     */
    function loadAnomalies(boxes) {
        anomalyBoxes = boxes || [];
        redraw();
    }

    function getAnomalyCount() {
        return anomalyBoxes.length;
    }

    // Public API
    return {
        initialize,
        setMode,
        setZoom,
        getZoom,
        resetView,
        clearAll,
        undoLastBox,
        loadAnomalies,
        getAnomalyCount
    };
})();

// Global helper for opening report windows
window.constWindowOpen = function (htmlContent) {
    const win = window.open('', '_blank');
    if (win) {
        win.document.write(htmlContent);
        win.document.close();
    } else {
        alert("Please allow popups to view the report.");
    }
};
