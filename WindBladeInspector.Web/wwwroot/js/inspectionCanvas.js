/**
 * QualiMax Blade Inspection Canvas
 * Vanilla JavaScript for handling canvas operations with zoom/pan/draw functionality.
 * 
 * Key Features:
 * - Zoom via mouse wheel with CSS transform
 * - Pan via click-and-drag
 * - Calibration mode: draws blue line (Root → Tip)
 * - Inspection mode: draws red annotation quadrilaterals (4-point polygons)
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

    // Coordinates for 4-point drawing
    let startX = 0, startY = 0;
    let currentX = 0, currentY = 0;
    let polygonPoints = []; // Array of {x, y} for multi-point drawing
    let drawingPolygon = false; // True when in 4-point mode

    // Pan/Zoom state
    let scale = 1.0;
    let panX = 0, panY = 0;
    let lastPanX = 0, lastPanY = 0;
    let minScale = 0.25; // Minimum scale - updated on setup to initial fit

    // Stored drawings
    let calibrationLine = null;
    let rootPoint = null;
    let anomalyBoxes = []; // Now stores polygons with 4 points

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

        console.log('QualiMax Canvas initialized with 4-point polygon support');
        return true;
    }

    function setupCanvas() {
        canvas.width = imageEl.naturalWidth;
        canvas.height = imageEl.naturalHeight;
        resetView(); // Use shared logic for initial fit
        minScale = scale; // Store the initial fit as minimum zoom
        console.log(`Canvas setup complete. Image: ${canvas.width}x${canvas.height}, Initial scale: ${minScale.toFixed(3)}`);
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
                canvas.style.cursor = drawingPolygon ? 'crosshair' : 'crosshair';
                break;
            case 'pan':
                canvas.style.cursor = isPanning ? 'grabbing' : 'grab';
                break;
            default:
                canvas.style.cursor = 'default';
        }
    }

    /**
     * Zoom handler via mouse wheel with smart zoom-to-cursor and minimum constraint.
     */
    function onWheel(e) {
        e.preventDefault();
        const delta = e.deltaY > 0 ? -0.1 : 0.1;

        // Prevent zooming out below initial fit (minScale)
        const newScale = Math.max(0.1, Math.min(scale + delta, 10.0));

        if (newScale === scale) return; // No change

        // Zoom towards cursor position
        const rect = containerEl.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        // Adjust pan to zoom towards mouse
        const scaleFactor = newScale / scale;
        panX = mouseX - (mouseX - panX) * scaleFactor;
        panY = mouseY - (mouseY - panY) * scaleFactor;

        scale = newScale;
        constrainPan(); // Apply pan constraints
        applyTransform();

        // Notify C# of zoom change
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('ReceiveZoomChange', scale);
        }
    }

    /**
     * Constrain pan so image never goes completely off-screen.
     * Allows dragging up to 80% of image off each edge.
     */
    function constrainPan() {
        if (!canvas || !containerEl) return;

        const containerRect = containerEl.getBoundingClientRect();
        const containerW = containerRect.width;
        const containerH = containerRect.height;
        const scaledW = canvas.width * scale;
        const scaledH = canvas.height * scale;

        // Allow dragging up to 80% of image off each edge
        const maxOffsetX = scaledW * 0.8;
        const maxOffsetY = scaledH * 0.8;

        // Constrain panX: right edge max, left edge min
        panX = Math.min(maxOffsetX, Math.max(containerW - scaledW - maxOffsetX, panX));
        panY = Math.min(maxOffsetY, Math.max(containerH - scaledH - maxOffsetY, panY));
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
        scale = Math.max(0.1, Math.min(newScale, 10.0));
        constrainPan();
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

        // Remove padding - fill the entire space
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

        if (mode === 'pan' || e.button === 1 || (e.button === 0 && e.altKey)) {
            // Middle mouse or Alt+Click = Pan
            isPanning = true;
            lastPanX = e.clientX - panX;
            lastPanY = e.clientY - panY;
            canvas.style.cursor = 'grabbing';
        } else if (mode === 'inspect') {
            // 4-point polygon drawing
            polygonPoints.push({ x: pos.x, y: pos.y });
            drawingPolygon = true;

            console.log(`Point ${polygonPoints.length} added at (${pos.x.toFixed(0)}, ${pos.y.toFixed(0)})`);

            if (polygonPoints.length === 4) {
                // Complete the polygon
                handleInspectionEnd();
            } else {
                // Redraw to show current points
                redraw();
                drawPolygonPreview();
            }
        } else if (mode === 'calibrate') {
            startX = pos.x;
            startY = pos.y;
            isDrawing = true;
            currentX = startX;
            currentY = startY;
        }
    }

    function onMouseMove(e) {
        if (isPanning) {
            panX = e.clientX - lastPanX;
            panY = e.clientY - lastPanY;
            constrainPan(); // Apply constraints during panning
            applyTransform();
            return;
        }

        if (mode === 'calibrate' && isDrawing) {
            const pos = getMousePos(e);
            currentX = pos.x;
            currentY = pos.y;
            redraw();
            drawPreview();
        } else if (mode === 'inspect' && drawingPolygon && polygonPoints.length > 0) {
            // Show preview line to next point
            const pos = getMousePos(e);
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
            currentX = pos.x;
            currentY = pos.y;
            handleCalibrationEnd();
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
        if (polygonPoints.length !== 4) return;

        // Calculate bounding box and area
        const bounds = getPolygonBounds(polygonPoints);
        const area = calculatePolygonArea(polygonPoints);

        console.log(`Polygon complete: Area = ${area.toFixed(2)} px², Bounds = ${bounds.width.toFixed(0)}x${bounds.height.toFixed(0)}`);

        const newBox = {
            points: [...polygonPoints],
            id: Date.now(),
            bounds: bounds,
            area: area
        };
        anomalyBoxes.push(newBox);

        redraw();

        if (dotNetRef) {
            // Send polygon points to C#
            dotNetRef.invokeMethodAsync('ReceivePolygonSelection',
                polygonPoints.map(p => p.x),
                polygonPoints.map(p => p.y),
                area,
                bounds.width,
                bounds.height,
                canvas.width,
                canvas.height);
        }

        // Reset for next polygon
        polygonPoints = [];
        drawingPolygon = false;
    }

    /**
     * Calculate polygon area using Shoelace formula
     */
    function calculatePolygonArea(points) {
        if (points.length < 3) return 0;

        let area = 0;
        for (let i = 0; i < points.length; i++) {
            const j = (i + 1) % points.length;
            area += points[i].x * points[j].y;
            area -= points[j].x * points[i].y;
        }
        return Math.abs(area / 2);
    }

    /**
     * Get bounding box of polygon
     */
    function getPolygonBounds(points) {
        if (points.length === 0) return { x: 0, y: 0, width: 0, height: 0 };

        let minX = points[0].x, maxX = points[0].x;
        let minY = points[0].y, maxY = points[0].y;

        for (let p of points) {
            minX = Math.min(minX, p.x);
            maxX = Math.max(maxX, p.x);
            minY = Math.min(minY, p.y);
            maxY = Math.max(maxY, p.y);
        }

        return {
            x: minX,
            y: minY,
            width: maxX - minX,
            height: maxY - minY
        };
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
     * Cancel current drawing in progress
     */
    function cancelCurrentDrawing() {
        polygonPoints = [];
        drawingPolygon = false;
        redraw();
        console.log('Current polygon drawing cancelled');
    }

    /**
     * Redraw all stored annotations.
     */
    function redraw() {
        if (!ctx) return;
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        // Draw anomaly polygons (red)
        anomalyBoxes.forEach((box, idx) => {
            if (box.points && box.points.length === 4) {
                drawPolygon(box.points, '#ff0000', 'rgba(255, 0, 0, 0.2)', 3);

                // Label at first point
                ctx.fillStyle = '#ff0000';
                ctx.font = 'bold 16px Inter, sans-serif';
                ctx.fillText(`#${idx + 1}`, box.points[0].x + 8, box.points[0].y + 22);
            }
        });
    }

    /**
     * Draw a polygon on canvas
     */
    function drawPolygon(points, strokeColor, fillColor, lineWidth) {
        if (points.length < 3) return;

        ctx.beginPath();
        ctx.moveTo(points[0].x, points[0].y);
        for (let i = 1; i < points.length; i++) {
            ctx.lineTo(points[i].x, points[i].y);
        }
        ctx.closePath();

        if (fillColor) {
            ctx.fillStyle = fillColor;
            ctx.fill();
        }

        if (strokeColor) {
            ctx.strokeStyle = strokeColor;
            ctx.lineWidth = lineWidth || 2;
            ctx.stroke();
        }
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
        }
    }

    /**
     * Draw polygon preview (lines connecting clicked points + preview to cursor)
     */
    function drawPolygonPreview(cursorPos) {
        if (polygonPoints.length === 0) return;

        // Draw existing points as circles
        polygonPoints.forEach((point, idx) => {
            ctx.beginPath();
            ctx.arc(point.x, point.y, 5, 0, Math.PI * 2);
            ctx.fillStyle = '#ff0000';
            ctx.fill();
            ctx.strokeStyle = '#fff';
            ctx.lineWidth = 2;
            ctx.stroke();

            // Number label
            ctx.fillStyle = '#ff0000';
            ctx.font = 'bold 14px Inter, sans-serif';
            ctx.fillText(`${idx + 1}`, point.x + 10, point.y - 10);
        });

        // Draw lines between points
        if (polygonPoints.length > 1) {
            ctx.beginPath();
            ctx.moveTo(polygonPoints[0].x, polygonPoints[0].y);
            for (let i = 1; i < polygonPoints.length; i++) {
                ctx.lineTo(polygonPoints[i].x, polygonPoints[i].y);
            }
            ctx.strokeStyle = '#ff0000';
            ctx.lineWidth = 2;
            ctx.setLineDash([8, 4]);
            ctx.stroke();
            ctx.setLineDash([]);
        }

        // Draw preview line to cursor
        if (cursorPos && polygonPoints.length < 4) {
            ctx.beginPath();
            ctx.moveTo(polygonPoints[polygonPoints.length - 1].x, polygonPoints[polygonPoints.length - 1].y);
            ctx.lineTo(cursorPos.x, cursorPos.y);
            ctx.strokeStyle = '#ff6666';
            ctx.lineWidth = 1;
            ctx.setLineDash([4, 4]);
            ctx.stroke();
            ctx.setLineDash([]);
        }

        // Draw preview closing line (for 3 points)
        if (polygonPoints.length === 3 && cursorPos) {
            ctx.beginPath();
            ctx.moveTo(cursorPos.x, cursorPos.y);
            ctx.lineTo(polygonPoints[0].x, polygonPoints[0].y);
            ctx.strokeStyle = '#ff9999';
            ctx.lineWidth = 1;
            ctx.setLineDash([2, 4]);
            ctx.stroke();
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
        polygonPoints = [];
        drawingPolygon = false;
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

    /**
     * Remove the box at a specific index (for syncing with Blazor)
     */
    function removeBoxByIndex(index) {
        if (index >= 0 && index < anomalyBoxes.length) {
            anomalyBoxes.splice(index, 1);
            redraw();
            return true;
        }
        return false;
    }

    function getAnomalyCount() {
        return anomalyBoxes.length;
    }

    /**
     * Clear all boxes immediately (used when canceling)
     */
    function clearLastDrawnBox() {
        if (anomalyBoxes.length > 0) {
            anomalyBoxes.pop();
            redraw();
            return true;
        }
        return false;
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
        getAnomalyCount,
        removeBoxByIndex,
        clearLastDrawnBox,
        cancelCurrentDrawing
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