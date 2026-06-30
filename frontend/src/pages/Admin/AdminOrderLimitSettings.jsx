import React, { useCallback, useEffect, useState } from "react";
import {
  getOrderLimitSettingsAdmin,
  updateOrderLimitSettings,
} from "../../services/orderLimitSettingsService";

const THEME = {
  primaryColor: "#f57c00",
  gradientHeader: "linear-gradient(135deg, #f57c00, #ff9800)",
};

const initialForm = {
  defaultMaxQuantityPiece: "5",
  defaultMinQuantityPiece: "1",
  defaultQuantityStepPiece: "1",
  defaultMaxWeightKg: "10",
  defaultMinWeightKg: "0.25",
  defaultWeightStepKg: "0.25",
};

export default function AdminOrderLimitSettings() {
  const [settings, setSettings] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editForm, setEditForm] = useState(initialForm);
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("success");

  const showMessage = (msg, type = "success") => {
    setMessage(msg);
    setMessageType(type);
    setTimeout(() => setMessage(""), 4000);
  };

  const loadSettings = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getOrderLimitSettingsAdmin();
      setSettings(data);
    } catch (err) {
      showMessage(err.message || "Ayarlar yüklenemedi", "danger");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadSettings();
  }, [loadSettings]);

  const handleStartEdit = () => {
    setIsEditing(true);
    setEditForm({
      defaultMaxQuantityPiece: String(settings?.defaultMaxQuantityPiece ?? 5),
      defaultMinQuantityPiece: String(settings?.defaultMinQuantityPiece ?? 1),
      defaultQuantityStepPiece: String(settings?.defaultQuantityStepPiece ?? 1),
      defaultMaxWeightKg: String(settings?.defaultMaxWeightKg ?? 10),
      defaultMinWeightKg: String(settings?.defaultMinWeightKg ?? 0.25),
      defaultWeightStepKg: String(settings?.defaultWeightStepKg ?? 0.25),
    });
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      await updateOrderLimitSettings({
        defaultMaxQuantityPiece: parseInt(editForm.defaultMaxQuantityPiece, 10),
        defaultMinQuantityPiece: parseInt(editForm.defaultMinQuantityPiece, 10),
        defaultQuantityStepPiece: parseFloat(editForm.defaultQuantityStepPiece),
        defaultMaxWeightKg: parseFloat(editForm.defaultMaxWeightKg),
        defaultMinWeightKg: parseFloat(editForm.defaultMinWeightKg),
        defaultWeightStepKg: parseFloat(editForm.defaultWeightStepKg),
      });
      showMessage("Sipariş limit ayarları güncellendi");
      setIsEditing(false);
      await loadSettings();
    } catch (err) {
      showMessage(
        err.response?.data?.message || err.message || "Kaydetme başarısız",
        "danger",
      );
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="container-fluid py-4 text-center">
        <div className="spinner-border" style={{ color: THEME.primaryColor }} />
      </div>
    );
  }

  const renderField = (name, label, step = "1") => (
    <div className="col-md-4 mb-3">
      <label className="form-label fw-semibold">{label}</label>
      <input
        type="number"
        className="form-control"
        name={name}
        step={step}
        min="0"
        value={editForm[name]}
        onChange={(e) =>
          setEditForm((prev) => ({ ...prev, [name]: e.target.value }))
        }
        disabled={!isEditing}
      />
    </div>
  );

  return (
    <div className="container-fluid py-3 py-md-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h2 className="mb-1 fw-bold">
            <i className="fas fa-sliders-h me-2" style={{ color: THEME.primaryColor }} />
            Sipariş Limit Ayarları
          </h2>
          <p className="text-muted mb-0">
            Tüm ürünler için varsayılan adet/kg limitleri (ürün bazlı override ile ezilebilir)
          </p>
        </div>
        {!isEditing ? (
          <button className="btn btn-primary" onClick={handleStartEdit}>
            <i className="fas fa-edit me-1" /> Düzenle
          </button>
        ) : (
          <div className="d-flex gap-2">
            <button className="btn btn-success" onClick={handleSave} disabled={saving}>
              {saving ? "Kaydediliyor..." : "Kaydet"}
            </button>
            <button
              className="btn btn-outline-secondary"
              onClick={() => setIsEditing(false)}
              disabled={saving}
            >
              İptal
            </button>
          </div>
        )}
      </div>

      {message && (
        <div className={`alert alert-${messageType}`}>{message}</div>
      )}

      <div className="row g-4">
        <div className="col-lg-6">
          <div className="card border-0 shadow-sm">
            <div className="card-header text-white" style={{ background: THEME.gradientHeader }}>
              <strong>Adet Bazlı Ürünler</strong>
            </div>
            <div className="card-body row">
              {renderField("defaultMinQuantityPiece", "Min Adet")}
              {renderField("defaultMaxQuantityPiece", "Max Adet")}
              {renderField("defaultQuantityStepPiece", "Adım", "0.01")}
            </div>
          </div>
        </div>
        <div className="col-lg-6">
          <div className="card border-0 shadow-sm">
            <div className="card-header text-white" style={{ background: THEME.gradientHeader }}>
              <strong>Kg (Tartılı) Ürünler</strong>
            </div>
            <div className="card-body row">
              {renderField("defaultMinWeightKg", "Min Kg", "0.01")}
              {renderField("defaultMaxWeightKg", "Max Kg", "0.01")}
              {renderField("defaultWeightStepKg", "Adım (kg)", "0.01")}
            </div>
          </div>
        </div>
      </div>

      {settings?.updatedAt && (
        <p className="text-muted small mt-3">
          Son güncelleme: {new Date(settings.updatedAt).toLocaleString("tr-TR")}
          {settings.updatedByUserName ? ` — ${settings.updatedByUserName}` : ""}
        </p>
      )}
    </div>
  );
}
