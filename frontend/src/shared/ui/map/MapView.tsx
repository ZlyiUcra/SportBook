import React from 'react'
import { MapContainer, Marker, Popup, TileLayer, useMapEvents } from 'react-leaflet'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
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

export type LatLng = { lat: number; lng: number }

export type MapMarker = {
  id: string
  position: LatLng
  /** Rendered as react-leaflet JSX children only - never raw HTML (contract MUST, research.md Map content safety). */
  popup?: React.ReactNode
}

type MapViewProps = {
  center: LatLng
  zoom?: number
  markers?: MapMarker[]
  /** When set, clicking the map reports the clicked position - used by the owner pin-picker (US2). */
  onPick?: (position: LatLng) => void
  className?: string
}

function ClickHandler({ onPick }: { onPick: (position: LatLng) => void }) {
  useMapEvents({
    click(e) {
      onPick({ lat: e.latlng.lat, lng: e.latlng.lng })
    },
  })
  return null
}

/**
 * Typed Leaflet wrapper - the only module in the app importing `leaflet`/`react-leaflet`
 * (plan.md Project Structure). Consumers must always load this via `React.lazy`/dynamic
 * `import()` so the map stack never lands in an initial route chunk (spec SC-006).
 */
export default function MapView({ center, zoom = 13, markers = [], onPick, className }: MapViewProps) {
  return (
    <MapContainer center={[center.lat, center.lng]} zoom={zoom} className={className ?? 'h-64 w-full'}>
      <TileLayer url={mapTiles.tileUrl} attribution={mapTiles.attribution} />
      {onPick && <ClickHandler onPick={onPick} />}
      {markers.map((marker) => (
        <Marker key={marker.id} position={[marker.position.lat, marker.position.lng]} icon={defaultIcon}>
          {marker.popup && <Popup>{marker.popup}</Popup>}
        </Marker>
      ))}
    </MapContainer>
  )
}
