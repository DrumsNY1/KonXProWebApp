// Leaflet.js interop for Blazor Server
// Manages the permit map with colored markers
window.leafletInterop = {
    map: null,
    markerLayer: null,

    initializeMap: function (elementId, lat, lng, zoom) {
        if (this.map) {
            this.map.remove();
        }

        this.map = L.map(elementId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        }).addTo(this.map);

        this.markerLayer = L.layerGroup().addTo(this.map);
    },

    addMarkers: function (markersJson, dotNetRef) {
        if (!this.map || !this.markerLayer) return;

        var markers = JSON.parse(markersJson);

        markers.forEach(function (m) {
            // Color by job status
            var color;
            switch (m.status) {
                case 'D': case 'E': case 'F': // Filed stages
                    color = '#4CAF50'; // Green
                    break;
                case 'G': case 'H': case 'I': case 'J': // Approved stages
                    color = '#FF9800'; // Orange/Yellow
                    break;
                case 'R': case 'X': // Permitted / Signed off
                    color = '#2196F3'; // Blue
                    break;
                default:
                    color = '#9E9E9E'; // Grey
            }

            var icon = L.divIcon({
                className: 'permit-marker',
                html: '<div style="background-color: ' + color + '; width: 12px; height: 12px; border-radius: 50%; border: 2px solid white; box-shadow: 0 1px 3px rgba(0,0,0,0.3);"></div>',
                iconSize: [16, 16],
                iconAnchor: [8, 8]
            });

            var stars = '';
            for (var i = 0; i < m.score; i++) stars += '★';

            var popup = '<div style="min-width: 200px;">' +
                '<strong>' + m.title + '</strong><br/>' +
                '<span style="color: #666;">' + m.borough + '</span><br/>' +
                'Type: <strong>' + m.jobType + '</strong> | Cost: <strong>' + m.cost + '</strong><br/>' +
                'Score: <span style="color: #FFB400;">' + stars + '</span><br/>' +
                '<a href="javascript:void(0)" onclick="leafletInterop.markerClick(' + m.id + ')" style="color: #1976D2;">View Details →</a>' +
                '</div>';

            L.marker([m.lat, m.lng], { icon: icon })
                .bindPopup(popup)
                .addTo(leafletInterop.markerLayer);
        });

        // Store dotNetRef for callbacks
        this._dotNetRef = dotNetRef;
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
        }
    }
};
