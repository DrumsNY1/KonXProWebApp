// Leaflet.js interop for Blazor Server - Interactive Permit Map
window.leafletInterop = {
    map: null,
    markerLayer: null,
    legendControl: null,
    _dotNetRef: null,

    initializeMap: function (elementId, lat, lng, zoom) {
        if (this.map) {
            this.destroyMap();
        }

        this.map = L.map(elementId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        }).addTo(this.map);

        this.markerLayer = L.layerGroup().addTo(this.map);
        this.addLegend();
    },

    addLegend: function () {
        if (!this.map || this.legendControl) return;

        var legend = L.control({ position: 'bottomright' });

        legend.onAdd = function () {
            var div = L.DomUtil.create('div', 'leaflet-map-legend');
            div.style.backgroundColor = 'rgba(15, 23, 42, 0.9)';
            div.style.color = '#fff';
            div.style.padding = '10px 14px';
            div.style.borderRadius = '8px';
            div.style.boxShadow = '0 4px 12px rgba(0,0,0,0.3)';
            div.style.fontSize = '12px';
            div.style.border = '1px solid rgba(255,255,255,0.1)';

            div.innerHTML =
                '<div style="font-weight: 700; margin-bottom: 6px; font-size: 13px;">Lead Quality Tiers</div>' +
                '<div style="display:flex; align-items:center; gap:8px; margin-bottom:4px;"><span style="background:#EF4444; width:12px; height:12px; border-radius:50%; display:inline-block; box-shadow:0 0 8px #EF4444;"></span> <strong>🔥 Hot Lead</strong> (Score 4-5)</div>' +
                '<div style="display:flex; align-items:center; gap:8px; margin-bottom:4px;"><span style="background:#F59E0B; width:12px; height:12px; border-radius:50%; display:inline-block;"></span> <strong>⚡ High Priority</strong> (Score 3)</div>' +
                '<div style="display:flex; align-items:center; gap:8px;"><span style="background:#3B82F6; width:12px; height:12px; border-radius:50%; display:inline-block;"></span> <strong>📋 Standard Lead</strong> (Score 1-2)</div>';

            return div;
        };

        legend.addTo(this.map);
        this.legendControl = legend;
    },

    addMarkers: function (markersJson, dotNetRef) {
        if (!this.map || !this.markerLayer) return;

        this._dotNetRef = dotNetRef;
        this.clearMarkers();

        var markers = typeof markersJson === 'string' ? JSON.parse(markersJson) : markersJson;
        if (!markers || markers.length === 0) return;

        var bounds = [];

        markers.forEach(function (m) {
            var color = '#3B82F6';
            var glowClass = '';
            var tierLabel = 'STANDARD';

            if (m.score >= 4) {
                color = '#EF4444'; // Red
                glowClass = 'marker-glow-red';
                tierLabel = '🔥 HOT LEAD';
            } else if (m.score === 3) {
                color = '#F59E0B'; // Amber
                glowClass = 'marker-glow-amber';
                tierLabel = '⚡ HIGH PRIORITY';
            }

            var iconHtml = '<div class="custom-permit-pin ' + glowClass + '" style="background-color: ' + color + '; width: 16px; height: 16px; border-radius: 50%; border: 2px solid white; box-shadow: 0 2px 6px rgba(0,0,0,0.4);">' +
                '</div>';

            var icon = L.divIcon({
                className: 'permit-marker-wrapper',
                html: iconHtml,
                iconSize: [20, 20],
                iconAnchor: [10, 10]
            });

            var stars = '';
            for (var i = 0; i < m.score; i++) stars += '★';
            for (var j = m.score; j < 5; j++) stars += '☆';

            var factorsHtml = '';
            if (m.factors && m.factors.length > 0) {
                factorsHtml = '<div style="margin-top: 6px; padding-top: 6px; border-top: 1px dashed #e2e8f0; display: flex; flex-wrap: wrap; gap: 4px;">';
                m.factors.forEach(function (f) {
                    factorsHtml += '<span style="background: #f1f5f9; color: #334155; padding: 2px 6px; border-radius: 4px; font-size: 10px; font-weight: 600;">+' + f.points + ' ' + f.name + '</span>';
                });
                factorsHtml += '</div>';
            }

            var popupContent =
                '<div style="min-width: 220px; font-family: Inter, sans-serif; padding: 2px;">' +
                '<div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px;">' +
                '<span style="font-weight: 700; font-size: 14px; color: #0f172a;">' + m.title + '</span>' +
                '<span style="font-size: 10px; font-weight: 700; background: ' + color + '; color: white; padding: 2px 6px; border-radius: 4px;">' + tierLabel + '</span>' +
                '</div>' +
                '<div style="font-size: 11px; color: #64748b; margin-bottom: 6px;">' + m.borough + ' • BIN ' + (m.bin || 'N/A') + '</div>' +
                '<div style="font-size: 12px; margin-bottom: 4px;"><strong>Job Type:</strong> ' + m.jobType + ' | <strong>Cost:</strong> <span style="color:#059669; font-weight:700;">' + m.cost + '</span></div>' +
                '<div style="font-size: 12px; margin-bottom: 6px;"><strong>Lead Rating:</strong> <span style="color: #F59E0B; font-weight:700;">' + stars + ' (' + m.score + '/5)</span></div>' +
                factorsHtml +
                '<div style="margin-top: 10px; text-align: right;">' +
                '<button onclick="leafletInterop.markerClick(' + m.id + ')" style="background: #2563EB; color: white; border: none; padding: 5px 12px; border-radius: 4px; font-size: 11px; font-weight: 600; cursor: pointer; transition: background 0.2s;">View Permit Detail →</button>' +
                '</div>' +
                '</div>';

            var marker = L.marker([m.lat, m.lng], { icon: icon }).bindPopup(popupContent);
            leafletInterop.markerLayer.addLayer(marker);
            bounds.push([m.lat, m.lng]);
        });

        if (bounds.length > 0) {
            try {
                this.map.fitBounds(bounds, { padding: [40, 40], maxZoom: 15 });
            } catch (e) { }
        }
    },

    panToLocation: function (lat, lng, zoom) {
        if (this.map) {
            this.map.flyTo([lat, lng], zoom || 13, { duration: 1.2 });
        }
    },

    markerClick: function (permitId) {
        if (this._dotNetRef) {
            this._dotNetRef.invokeMethodAsync('OnMarkerClicked', permitId);
        }
    },

    clearMarkers: function () {
        if (this.markerLayer) {
            this.markerLayer.clearLayers();
        }
    },

    destroyMap: function () {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.markerLayer = null;
            this.legendControl = null;
        }
    }
};

window.saveAsFile = function (filename, base64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = 'data:text/csv;base64,' + base64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
