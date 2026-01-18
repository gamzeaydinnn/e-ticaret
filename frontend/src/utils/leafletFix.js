/**
 * leafletFix.js - Leaflet İkon Düzeltmesi
 *
 * NEDEN: Webpack ile Leaflet ikonları çakışır, default ikonlar yüklenmez.
 * Bu fix, ikonları manuel olarak import edip Leaflet'e tanıtır.
 */

import L from "leaflet";
import icon from "leaflet/dist/images/marker-icon.png";
import iconShadow from "leaflet/dist/images/marker-shadow.png";
import iconRetina from "leaflet/dist/images/marker-icon-2x.png";

// Leaflet'in default icon URL metodunu sil (webpack ile çakışıyor)
delete L.Icon.Default.prototype._getIconUrl;

// İkonları manuel olarak tanımla
L.Icon.Default.mergeOptions({
  iconUrl: icon,
  iconRetinaUrl: iconRetina,
  shadowUrl: iconShadow,
});

export default L;
