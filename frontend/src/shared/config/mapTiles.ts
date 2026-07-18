/**
 * Single switch point for the map tile provider (research.md Tile provider decision). Pre-
 * production uses the public OSM tile server with its required attribution; swapping to a keyed,
 * Origin-restricted provider before release means changing only this object.
 */
export const mapTiles = {
  tileUrl: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
  attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
}
