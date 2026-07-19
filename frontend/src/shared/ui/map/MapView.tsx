import React from 'react'
import { MapContainer, Marker, Popup, TileLayer, useMap, useMapEvents } from 'react-leaflet'
import MarkerClusterGroup from 'react-leaflet-cluster'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import 'leaflet.markercluster/dist/MarkerCluster.css'
import 'leaflet.markercluster/dist/MarkerCluster.Default.css'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'
import { mapTiles } from '@/shared/config/mapTiles'

// Leaflet's default marker icon references image paths that break under most bundlers - point it
// at the Vite-processed asset URLs instead (a one-time, module-level fix, not per-marker).
const defaultIcon = L.icon({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
})

// The nearest-venue emphasis marker (003 research.md "Marker emphasis for the nearest venue") -
// a second, larger `L.icon` using the same asset, never `L.divIcon({ html })` fed from venue
// fields (stored-XSS avoidance, contracts/api.md Frontend contract MUSTs).
const emphasizedIcon = L.icon({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
  iconSize: [37, 61],
  iconAnchor: [18, 61],
  popupAnchor: [1, -50],
  shadowSize: [61, 61],
})

export type LatLng = { lat: number; lng: number }

/**
 * Plain serializable viewport shape (004 data-model.md "MapBounds") - the only form in which the
 * visible area crosses this module's boundary, so no Leaflet type leaks into pages (contract
 * MUST). A point is visible when lat in [south, north] and lng in [west, east].
 */
export type MapBounds = { south: number; west: number; north: number; east: number }

export type MapMarker = {
  id: string
  position: LatLng
  /** Rendered as react-leaflet JSX children only - never raw HTML (contract MUST, research.md Map content safety). */
  popup?: React.ReactNode
  /** Renders this marker with the larger `emphasizedIcon` (e.g. the nearest venue in a radius view). */
  emphasized?: boolean
}

type MapViewProps = {
  center: LatLng
  zoom?: number
  markers?: MapMarker[]
  /** When set, clicking the map reports the clicked position - used by the owner pin-picker (US2). */
  onPick?: (position: LatLng) => void
  className?: string
  /** Groups nearby markers into an expanding count bubble via react-leaflet-cluster (003 US1). */
  cluster?: boolean
  /**
   * When set, the map frames all current markers to fit the view once per change of this key
   * (e.g. an identity string combining the reference point and the returned venue-id set) -
   * never on unrelated re-renders, which would fight manual zoom/pan (research.md fitBounds
   * behaviour).
   */
  fitBoundsKey?: string
  /** Caps how far `fitBoundsKey` framing may zoom in, so a tight cluster of markers does not over-zoom. */
  maxFitZoom?: number
  /**
   * Reports the visible area after each completed zoom/pan gesture (`moveend`/`zoomend` - never
   * during one, 004 spec FR-008) and once on mount, as a plain `MapBounds`. Drives the
   * viewport-synced results list (004 US2).
   */
  onViewportChange?: (bounds: MapBounds) => void
}

function ClickHandler({ onPick }: { onPick: (position: LatLng) => void }) {
  useMapEvents({
    click(e) {
      onPick({ lat: e.latlng.lat, lng: e.latlng.lng })
    },
  })
  return null
}

/** Converts Leaflet's bounds object to the plain boundary-crossing shape (004 contract MUST). */
function toMapBounds(map: L.Map): MapBounds {
  const bounds = map.getBounds()
  return { south: bounds.getSouth(), west: bounds.getWest(), north: bounds.getNorth(), east: bounds.getEast() }
}

/**
 * Reports the viewport on completed gestures only - `moveend`/`zoomend` fire once per finished
 * drag/zoom, so no debouncing is needed (004 research.md "Viewport reporting"). The mount-time
 * report gives consumers real bounds before any gesture; `fitBounds` framing then emits its own
 * `moveend` with the framed view. The callback is read through a ref so re-renders of the parent
 * never re-subscribe the map events.
 */
function ViewportReporter({ onViewportChange }: { onViewportChange: (bounds: MapBounds) => void }) {
  const callbackRef = React.useRef(onViewportChange)
  callbackRef.current = onViewportChange

  const map = useMapEvents({
    moveend() {
      callbackRef.current(toMapBounds(map))
    },
    zoomend() {
      callbackRef.current(toMapBounds(map))
    },
  })

  React.useEffect(() => {
    callbackRef.current(toMapBounds(map))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return null
}

/**
 * `MapContainer` only reads `center`/`zoom` at mount, so framing all pins on a later
 * reference-point change needs an imperative effect. Markers are read via a ref (updated every
 * render) so the effect itself only re-runs when `fitBoundsKey` changes, not on every render.
 */
function FitBounds({ markers, fitBoundsKey, maxFitZoom }: { markers: MapMarker[]; fitBoundsKey: string; maxFitZoom: number }) {
  const map = useMap()
  const markersRef = React.useRef(markers)
  markersRef.current = markers

  React.useEffect(() => {
    const current = markersRef.current
    if (current.length === 0) return
    const bounds = L.latLngBounds(current.map((marker): [number, number] => [marker.position.lat, marker.position.lng]))
    map.fitBounds(bounds, { maxZoom: maxFitZoom })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fitBoundsKey])

  return null
}

/**
 * Typed Leaflet wrapper - the only module in the app importing `leaflet`/`react-leaflet`
 * (plan.md Project Structure). Consumers must always load this via `React.lazy`/dynamic
 * `import()` so the map stack never lands in an initial route chunk (spec SC-006).
 */
export default function MapView({
  center,
  zoom = 13,
  markers = [],
  onPick,
  className,
  cluster = false,
  fitBoundsKey,
  maxFitZoom = 16,
  onViewportChange,
}: MapViewProps) {
  const markerElements = markers.map((marker) => (
    <Marker
      key={marker.id}
      position={[marker.position.lat, marker.position.lng]}
      icon={marker.emphasized ? emphasizedIcon : defaultIcon}
    >
      {marker.popup && <Popup>{marker.popup}</Popup>}
    </Marker>
  ))

  return (
    <MapContainer center={[center.lat, center.lng]} zoom={zoom} className={className ?? 'h-64 w-full'}>
      <TileLayer url={mapTiles.tileUrl} attribution={mapTiles.attribution} />
      {onPick && <ClickHandler onPick={onPick} />}
      {onViewportChange && <ViewportReporter onViewportChange={onViewportChange} />}
      {fitBoundsKey !== undefined && <FitBounds markers={markers} fitBoundsKey={fitBoundsKey} maxFitZoom={maxFitZoom} />}
      {cluster ? <MarkerClusterGroup>{markerElements}</MarkerClusterGroup> : markerElements}
    </MapContainer>
  )
}
