import React, { useState, useEffect, useCallback, useMemo } from "react";
import { AdminService } from "../../services/adminService";
import { useAuth } from "../../contexts/AuthContext";
import { permissionService, ROLE_LABELS } from "../../services/permissionService";
// ============================================================================
// Yeni Bileşen İmportları - RBAC Security Complete Fix
// Arama, filtreleme, sayfalama ve hata mesajları için
// ============================================================================
import UserSearchFilter, {
  filterUsers,
} from "../../components/UserSearchFilter";
import Pagination, { paginateData } from "../../components/Pagination";
import { translateError } from "../../utils/errorMessages";
// Mobil uyumlu stiller
import "../../styles/adminUsers.css";

// ============================================================================
// Admin Paneline Erişim Yetkisi Olan Roller
// Backend'deki Roles.GetAdminPanelRoles() ile senkronize tutulmalı
// ============================================================================
const ADMIN_PANEL_ROLES = [
  "SuperAdmin",
  "Admin",
  "StoreManager",
  "CustomerSupport",
  "Logistics",
  "StoreAttendant", // Yeni: Market Görevlisi
  "Dispatcher", // Yeni: Sevkiyat Görevlisi
];

// ============================================================================
// 5 TEMEL ROL VE AÇIKLAMALARI
// RBAC (Role-Based Access Control) sistemi için tanımlanmış roller
// ============================================================================
const ROLE_DESCRIPTIONS = {
  SuperAdmin: {
    name: "Süper Yönetici",
    description:
      "Sistemin tam yetkili sahibidir. Tüm site ayarlarını değiştirme, diğer yöneticileri atama/silme, ödeme yöntemlerini yapılandırma ve tam veri dışa aktarma yetkisine sahiptir.",
    color: "danger",
    icon: "👑",
  },
  StoreManager: {
    name: "Mağaza Yöneticisi",
    description:
      "Günlük iş akışını yöneten kişidir. Ürün ekleme/güncelleme, stok yönetimi, kampanya ve kupon oluşturma, satış raporlarını görüntüleme yetkilerine sahiptir. Sistem ayarlarına erişemez.",
    color: "warning",
    icon: "🏪",
  },
  CustomerSupport: {
    name: "Müşteri Hizmetleri",
    description:
      "Müşteri memnuniyetini sağlamak ve sipariş sorunlarını çözmekle görevlidir. Sipariş durumlarını güncelleme, iade süreçlerini yönetme, müşteri yorumlarını onaylama yetkilerine sahiptir.",
    color: "info",
    icon: "🎧",
  },
  Logistics: {
    name: "Lojistik Görevlisi",
    description:
      "Depo ve kargo operasyonlarından sorumludur. Sadece gönderilmeyi bekleyen sipariş listesini görme ve kargo takip numarası girme yetkisine sahiptir. Müşteri bilgilerine erişemez.",
    color: "secondary",
    icon: "🚚",
  },
  // =========================================================================
  // YENİ ROLLER - Order-Courier-Panel Sistemi için
  // =========================================================================
  StoreAttendant: {
    name: "Market Görevlisi",
    description:
      "Sipariş hazırlama sürecinden sorumludur. Bekleyen siparişleri görme, hazırlamaya başla/hazır işaretleme, tartı girişi yapma yetkilerine sahiptir. Sadece Store Attendant paneline erişir.",
    color: "primary",
    icon: "📦",
  },
  Dispatcher: {
    name: "Sevkiyat Görevlisi",
    description:
      "Kurye atama ve takip sürecinden sorumludur. Hazır siparişleri görme, kurye atama/değiştirme, kurye listesini görüntüleme yetkilerine sahiptir. Sadece Dispatcher paneline erişir.",
    color: "success",
    icon: "🗂️",
  },
  Admin: {
    name: "Admin (Eski)",
    description:
      "[Deprecated] Eski uyumluluk için korunmuş rol. Yeni kullanıcılar için StoreManager tercih edilmeli.",
    color: "dark",
    icon: "⚙️",
  },
  User: {
    name: "Müşteri",
    description:
      "Sitenin son kullanıcısıdır. Ürün satın alma, kendi profilini düzenleme, sipariş geçmişini görüntüleme ve favori listesi oluşturma yetkilerine sahiptir.",
    color: "light",
    icon: "👤",
  },
  Customer: {
    name: "Müşteri",
    description:
      "Sitenin son kullanıcısıdır. Ürün satın alma, kendi profilini düzenleme, sipariş geçmişini görüntüleme yetkilerine sahiptir.",
    color: "light",
    icon: "👤",
  },
  Courier: {
    name: "Kurye",
    description:
      "Teslimat sürecinden sorumludur. Atanan siparişleri teslim alma, yola çıkma ve teslimat yapma yetkilerine sahiptir. Kurye paneline erişir.",
    color: "purple",
    icon: "🏍️",
  },
};

// Backend AllowedRoles ile senkron — API yüklenemezse fallback
const ASSIGNABLE_ROLE_NAMES = new Set([
  "SuperAdmin",
  "Admin",
  "StoreManager",
  "CustomerSupport",
  "Logistics",
  "StoreAttendant",
  "Dispatcher",
  "Courier",
  "User",
  "Customer",
]);

const PRIVILEGED_ASSIGNABLE_ROLES = new Set(["SuperAdmin", "Admin"]);

const FALLBACK_ASSIGNABLE_ROLES = [
  { value: "SuperAdmin", label: "Süper Yönetici", requiresSuperAdmin: true },
  { value: "Admin", label: "Admin (Eski)", requiresSuperAdmin: true },
  {
    value: "StoreManager",
    label: "Mağaza Yöneticisi",
    requiresSuperAdmin: false,
  },
  {
    value: "CustomerSupport",
    label: "Müşteri Hizmetleri",
    requiresSuperAdmin: false,
  },
  {
    value: "Logistics",
    label: "Lojistik Görevlisi",
    requiresSuperAdmin: false,
  },
  {
    value: "StoreAttendant",
    label: "Market Görevlisi",
    requiresSuperAdmin: false,
  },
  {
    value: "Dispatcher",
    label: "Sevkiyat Görevlisi",
    requiresSuperAdmin: false,
  },
  {
    value: "Courier",
    label: "Kurye",
    requiresSuperAdmin: false,
  },
  { value: "User", label: "Müşteri", requiresSuperAdmin: false },
  { value: "Customer", label: "Müşteri (Customer)", requiresSuperAdmin: false },
];

const mapApiRolesToAssignable = (roles) =>
  roles
    .map((role) => {
      const value = role.name || role.Name;
      if (!value || !ASSIGNABLE_ROLE_NAMES.has(value)) return null;
      return {
        value,
        label:
          role.displayName ||
          role.DisplayName ||
          ROLE_LABELS[value] ||
          value,
        requiresSuperAdmin: PRIVILEGED_ASSIGNABLE_ROLES.has(value),
      };
    })
    .filter(Boolean);

const AdminUsers = () => {
  const {
    user: currentUser,
    refreshPermissions,
    clearPermissionsCache,
    hasPermission,
  } = useAuth();
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedUser, setSelectedUser] = useState(null);
  const [selectedRole, setSelectedRole] = useState("User");
  const [saving, setSaving] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState("");
  // Kullanıcı oluşturma formu başlangıç değerleri
  // phoneNumber opsiyonel — boş bırakılabilir
  const initialCreateForm = {
    firstName: "",
    lastName: "",
    email: "",
    password: "",
    phoneNumber: "",
    address: "",
    city: "",
    role: "User",
  };
  const [createForm, setCreateForm] = useState(initialCreateForm);
  // ============================================================================
  // Kullanıcı Silme State'leri
  // Silme işlemi için onay modalı ve loading durumu
  // ============================================================================
  const [deleteConfirmUser, setDeleteConfirmUser] = useState(null);
  const [deleting, setDeleting] = useState(false);

  // ============================================================================
  // Arama, Filtreleme ve Sayfalama State'leri
  // UserSearchFilter ve Pagination bileşenleri için
  // ============================================================================
  const [filters, setFilters] = useState({
    search: "",
    role: "all",
    status: "all",
  });
  const [currentPage, setCurrentPage] = useState(1);
  const ITEMS_PER_PAGE = 20;

  // ============================================================================
  // Toplu İşlem (Bulk Actions) State'leri
  // Çoklu kullanıcı seçimi ve toplu işlemler için
  // ============================================================================
  const [selectedUserIds, setSelectedUserIds] = useState([]);
  const [bulkActionLoading, setBulkActionLoading] = useState(false);

  // ============================================================================
  // Kullanıcı Düzenleme State'leri
  // Admin panelinden kullanıcı bilgilerini (ad, soyad, email, telefon, adres, şehir) güncelleme
  // ============================================================================
  const [editModalUser, setEditModalUser] = useState(null);
  const [editForm, setEditForm] = useState({});
  const [editError, setEditError] = useState("");
  const [editing, setEditing] = useState(false);

  // ============================================================================
  // Madde 8: Şifre Güncelleme State'leri
  // Admin panelinden kullanıcı şifresi güncelleme için
  // ============================================================================
  const [passwordModalUser, setPasswordModalUser] = useState(null);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [passwordError, setPasswordError] = useState("");
  const [updatingPassword, setUpdatingPassword] = useState(false);

  // ============================================================================
  // Rol Değişikliği Uyarı State'i (Madde 5 düzeltmesi)
  // Kullanıcının rolü değiştirildiğinde gösterilecek uyarı
  // ============================================================================
  const [roleChangeWarning, setRoleChangeWarning] = useState(null);

  // ============================================================================
  // Dinamik RBAC İzin Matrisi State'leri
  // Backend'den çekilen rol-izin matrisi ile çalışır
  // ============================================================================
  const [matrixData, setMatrixData] = useState(null);
  const [matrixLoading, setMatrixLoading] = useState(false);
  const [matrixError, setMatrixError] = useState(null);
  const [matrixSaving, setMatrixSaving] = useState({});
  const [matrixExpandedModules, setMatrixExpandedModules] = useState({});
  const [matrixSaveSuccess, setMatrixSaveSuccess] = useState(null);
  const [assignableRoles, setAssignableRoles] = useState(FALLBACK_ASSIGNABLE_ROLES);

  // Admin yetkisi kontrolü - backend ile tutarlı olması için
  // SuperAdmin/Admin her zaman yetkiliyken, diğer roller permission tabanlı kontrol edilir
  const isAdminLike =
    currentUser?.role === "SuperAdmin" ||
    currentUser?.role === "Admin" ||
    currentUser?.role === "StoreManager";

  // İzin bazlı kontroller - frontend butonlarını backend ile senkron tutar
  const canCreateUser =
    currentUser?.role === "SuperAdmin" || hasPermission?.("users.create");
  const canUpdateUser =
    currentUser?.role === "SuperAdmin" || hasPermission?.("users.update");
  const canDeleteUser_perm =
    currentUser?.role === "SuperAdmin" || hasPermission?.("users.delete");
  const canManageRoles =
    currentUser?.role === "SuperAdmin" || hasPermission?.("users.roles");

  // ============================================================================
  // Filtrelenmiş ve Sayfalanmış Kullanıcı Listesi
  // Memoized hesaplama ile performans optimizasyonu
  // ============================================================================
  const filteredUsers = useMemo(() => {
    return filterUsers(users, filters);
  }, [users, filters]);

  const paginatedUsers = useMemo(() => {
    return paginateData(filteredUsers, currentPage, ITEMS_PER_PAGE);
  }, [filteredUsers, currentPage]);

  // Mevcut roller listesi (filtreleme için)
  const availableRoles = useMemo(() => {
    const roles = [...new Set(users.map((u) => u.role).filter(Boolean))];
    return roles.sort();
  }, [users]);

  // Filtre değiştiğinde ilk sayfaya dön
  const handleFilterChange = useCallback((newFilters) => {
    setFilters(newFilters);
    setCurrentPage(1);
    setSelectedUserIds([]); // Seçimleri temizle
  }, []);

  const loadUsers = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await AdminService.getUsers();
      const payload = response?.data || response;
      const list = Array.isArray(payload?.data)
        ? payload.data
        : Array.isArray(payload)
          ? payload
          : [];

      // ============================================================================
      // Sıralama: CreatedAt DESC — en yeni kullanıcı listenin başında
      // Backend sıralama garantisi olmadığı için frontend tarafında yapılır
      // ============================================================================
      const sorted = [...list].sort((a, b) => {
        const dateA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
        const dateB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
        return dateB - dateA;
      });
      setUsers(sorted);
    } catch (err) {
      console.error("Kullanıcılar yükleme hatası:", err);
      const status = err?.status || err?.response?.status;
      if (status === 401 || status === 403) {
        setError("Bu sayfayı görüntülemek için admin girişi yapmalısınız.");
      } else {
        setError("Kullanıcılar yüklenirken hata oluştu");
      }
    } finally {
      setLoading(false);
    }
  }, []);

  // ============================================================================
  // Dinamik RBAC Matrisi - Backend'den Yükleme
  // permissionService.getRolePermissionMatrix() ile çalışır
  // ============================================================================
  const loadMatrixData = useCallback(async () => {
    try {
      setMatrixLoading(true);
      setMatrixError(null);
      const data = await permissionService.getRolePermissionMatrix();

      // API yanıtını normalize et
      const headers = data?.PermissionHeaders || data?.permissionHeaders || [];
      const matrix = data?.RoleMatrix || data?.roleMatrix || [];

      setMatrixData({ permissionHeaders: headers, roleMatrix: matrix });

      // İlk yüklemede tüm modülleri aç
      if (headers.length > 0) {
        const expanded = {};
        headers.forEach((h) => {
          const mod = h.Module || h.module;
          if (mod) expanded[mod] = true;
        });
        setMatrixExpandedModules(expanded);
      }
    } catch (err) {
      console.error("[AdminUsers] Matris yükleme hatası:", err);
      setMatrixError("İzin matrisi yüklenirken hata oluştu. Lütfen tekrar deneyin.");
    } finally {
      setMatrixLoading(false);
    }
  }, []);

  // ============================================================================
  // İzin Toggle Handler - Tek checkbox tıklaması ile izin ekle/kaldır
  // Backend'e addPermissionToRole veya removePermissionFromRole çağrısı yapar
  // ============================================================================
  const handlePermissionToggle = useCallback(
    async (roleId, permissionId, currentHasPermission) => {
      // Kaydetme durumunu takip etmek için benzersiz anahtar
      const savingKey = `${roleId}_${permissionId}`;
      setMatrixSaving((prev) => ({ ...prev, [savingKey]: true }));
      setMatrixSaveSuccess(null);

      try {
        if (currentHasPermission) {
          await permissionService.removePermissionFromRole(roleId, permissionId);
        } else {
          await permissionService.addPermissionToRole(roleId, permissionId);
        }

        // Başarılı - local state'i güncelle (tekrar API çağırmadan)
        setMatrixData((prev) => {
          if (!prev) return prev;
          const updatedMatrix = prev.roleMatrix.map((role) => {
            const rId = role.RoleId || role.roleId;
            if (rId !== roleId) return role;
            return {
              ...role,
              Permissions: (role.Permissions || role.permissions || []).map((p) => {
                const pId = p.PermissionId || p.permissionId;
                if (pId !== permissionId) return p;
                return {
                  ...p,
                  HasPermission: !currentHasPermission,
                  hasPermission: !currentHasPermission,
                };
              }),
              permissions: (role.permissions || role.Permissions || []).map((p) => {
                const pId = p.permissionId || p.PermissionId;
                if (pId !== permissionId) return p;
                return {
                  ...p,
                  hasPermission: !currentHasPermission,
                  HasPermission: !currentHasPermission,
                };
              }),
            };
          });
          return { ...prev, roleMatrix: updatedMatrix };
        });

        setMatrixSaveSuccess("İzin başarıyla güncellendi.");
        setTimeout(() => setMatrixSaveSuccess(null), 2000);
      } catch (err) {
        console.error("[AdminUsers] İzin toggle hatası:", err);
        const msg =
          err?.response?.data?.message ||
          err?.message ||
          "İzin güncellenirken hata oluştu.";
        setMatrixError(msg);
        setTimeout(() => setMatrixError(null), 3000);
      } finally {
        setMatrixSaving((prev) => {
          const next = { ...prev };
          delete next[savingKey];
          return next;
        });
      }
    },
    [],
  );

  // Modül aç/kapa toggle
  const toggleMatrixModule = useCallback((moduleName) => {
    setMatrixExpandedModules((prev) => ({
      ...prev,
      [moduleName]: !prev[moduleName],
    }));
  }, []);

  const loadAssignableRoles = useCallback(async () => {
    try {
      const response = await AdminService.getRoles();
      const payload = response?.data || response;
      const list = Array.isArray(payload?.data)
        ? payload.data
        : Array.isArray(payload)
          ? payload
          : [];
      const mapped = mapApiRolesToAssignable(list);
      if (mapped.length > 0) {
        setAssignableRoles(mapped);
      }
    } catch (err) {
      console.warn("[AdminUsers] Rol listesi API'den yüklenemedi, fallback kullanılıyor:", err);
      setAssignableRoles(FALLBACK_ASSIGNABLE_ROLES);
    }
  }, []);

  useEffect(() => {
    loadUsers();
    loadAssignableRoles();
  }, [loadUsers, loadAssignableRoles]);

  // Dinamik matris verilerini yükle
  useEffect(() => {
    if (currentUser?.role === "SuperAdmin" || hasPermission?.("roles.view")) {
      loadMatrixData();
    }
  }, [loadMatrixData, currentUser, hasPermission]);

  const canEditUserRole = (u) => {
    if (!canManageRoles) return false;
    if (u?.id === currentUser?.id) return false;
    if (u?.role === "SuperAdmin" && currentUser?.role !== "SuperAdmin") {
      return false;
    }
    return true;
  };

  const openRoleModal = (u) => {
    setSelectedUser(u);
    setSelectedRole(u?.role || "User");
  };

  const closeRoleModal = () => {
    setSelectedUser(null);
    setSelectedRole("User");
  };

  const openCreateModal = () => {
    if (!canCreateUser) return;
    setCreateForm({
      ...initialCreateForm,
      role: "User",
    });
    setCreateError("");
    setShowCreateModal(true);
  };

  const closeCreateModal = () => {
    setShowCreateModal(false);
    setCreateForm(initialCreateForm);
    setCreateError("");
  };

  const handleCreateInputChange = (e) => {
    const { name, value } = e.target;
    setCreateForm((prev) => ({ ...prev, [name]: value }));
  };

  // Email format validasyonu için regex
  const isValidEmail = (email) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  };

  const handleCreateSubmit = async (e) => {
    e.preventDefault();
    if (!canCreateUser) return;

    // Zorunlu alan kontrolü
    if (
      !createForm.firstName.trim() ||
      !createForm.lastName.trim() ||
      !createForm.email.trim() ||
      !createForm.password.trim()
    ) {
      setCreateError("Ad, Soyad, Email ve Şifre alanları zorunludur.");
      return;
    }

    // Email format validasyonu
    if (!isValidEmail(createForm.email.trim())) {
      setCreateError(
        "Geçerli bir email adresi giriniz. (örn: kullanici@domain.com)",
      );
      return;
    }

    // Şifre minimum uzunluk kontrolü (en az 6 karakter)
    if (createForm.password.length < 6) {
      setCreateError("Şifre en az 6 karakter olmalıdır.");
      return;
    }

    const desiredRole = createForm.role || "User";
    if (
      (desiredRole === "SuperAdmin" || desiredRole === "Admin") &&
      currentUser?.role !== "SuperAdmin"
    ) {
      setCreateError(
        "SuperAdmin ve Admin rollerini atamak için SuperAdmin yetkisine sahip olmalısınız.",
      );
      return;
    }

    try {
      setCreating(true);
      setCreateError("");
      const payload = {
        firstName: createForm.firstName.trim(),
        lastName: createForm.lastName.trim(),
        email: createForm.email.trim(),
        password: createForm.password,
        phoneNumber: createForm.phoneNumber?.trim() || null,
        address: createForm.address?.trim() || null,
        city: createForm.city?.trim() || null,
        role: desiredRole,
      };
      await AdminService.createUser(payload);
      // ============================================================================
      // Kullanıcı başarıyla oluşturuldu — listeyi yenile ve ilk sayfaya dön
      // Yeni kullanıcı CreatedAt DESC sıralaması sayesinde listenin başında görünür
      // ============================================================================
      await loadUsers();
      setCurrentPage(1);
      closeCreateModal();
    } catch (err) {
      console.error("Kullanıcı oluşturma hatası:", err);
      // Türkçe hata mesajı çevirisi
      const errorMessage = translateError(
        err?.response?.data || err?.message || err,
      );
      setCreateError(errorMessage);
    } finally {
      setCreating(false);
    }
  };

  const handleSaveRole = async () => {
    if (!selectedUser) return;
    if (selectedUser.id === currentUser?.id) {
      alert("Kendi rolünüzü değiştiremezsiniz.");
      return;
    }
    try {
      setSaving(true);
      await AdminService.updateUserRole(selectedUser.id, selectedRole);

      // ============================================================================
      // Madde 5 Düzeltmesi: Rol Değişikliği Sonrası Cache Yönetimi
      // ============================================================================

      // 1. Eğer değiştirilen kullanıcı şu an giriş yapmış kullanıcı ise
      //    kendi izin cache'ini temizle ve yeniden yükle
      if (selectedUser.id === currentUser?.id) {
        clearPermissionsCache?.();
        await refreshPermissions?.();

        // Kullanıcıya bilgi ver - kendi rolü değişti
        setRoleChangeWarning({
          type: "self",
          message: `Rolünüz "${selectedRole}" olarak güncellendi. İzinleriniz yeniden yüklendi.`,
          userName:
            selectedUser.fullName ||
            `${selectedUser.firstName} ${selectedUser.lastName}`.trim(),
        });
      } else {
        // 2. Başka bir kullanıcının rolü değiştirildi
        //    O kullanıcı aktif oturumda ise, bir sonraki sayfa yenilemesinde
        //    veya logout/login'de yeni izinler yüklenecek
        //    Admin'e bilgi ver
        setRoleChangeWarning({
          type: "other",
          message: `"${
            selectedUser.fullName || selectedUser.email
          }" kullanıcısının rolü "${selectedRole}" olarak güncellendi. Kullanıcı aktif oturumdaysa, değişiklikler bir sonraki giriş veya sayfa yenilemesinde geçerli olacaktır.`,
          userName:
            selectedUser.fullName ||
            `${selectedUser.firstName} ${selectedUser.lastName}`.trim(),
        });
      }

      // UI'ı güncelle
      setUsers((prev) =>
        prev.map((u) =>
          u.id === selectedUser.id ? { ...u, role: selectedRole } : u,
        ),
      );
      closeRoleModal();

      // Uyarıyı 8 saniye sonra otomatik kapat
      setTimeout(() => setRoleChangeWarning(null), 8000);
    } catch (err) {
      console.error("Rol güncelleme hatası:", err);
      // Türkçe hata mesajı çevirisi
      const errorMessage = translateError(
        err?.response?.data || err?.message || err,
      );
      alert(errorMessage);
    } finally {
      setSaving(false);
    }
  };

  // ============================================================================
  // Kullanıcı Silme İşlemleri
  // Güvenlik: Kendi hesabını ve SuperAdmin'i silme engeli
  // Backend: AdminUsersController.DeleteUser endpoint'i (Users.Delete permission)
  // ============================================================================

  /**
   * Kullanıcının silinip silinemeyeceğini kontrol eder
   * @param {Object} u - Kontrol edilecek kullanıcı
   * @returns {boolean} - Silinebilir ise true
   */
  const canDeleteUser = (u) => {
    // Silme izni yoksa silme yapamaz
    if (!canDeleteUser_perm) return false;

    // Kendi hesabını silemez
    if (u?.id === currentUser?.id) return false;

    // SuperAdmin'i sadece SuperAdmin silebilir
    if (u?.role === "SuperAdmin" && currentUser?.role !== "SuperAdmin") {
      return false;
    }

    return true;
  };

  /**
   * Silme onay modalını açar
   * @param {Object} u - Silinecek kullanıcı
   */
  const openDeleteConfirm = (u) => {
    setDeleteConfirmUser(u);
  };

  /**
   * Silme onay modalını kapatır
   */
  const closeDeleteConfirm = () => {
    setDeleteConfirmUser(null);
  };

  /**
   * Kullanıcıyı siler
   * Backend'e DELETE /api/admin/users/{id} isteği gönderir
   */
  const handleDeleteUser = async () => {
    if (!deleteConfirmUser) return;

    try {
      setDeleting(true);
      await AdminService.deleteUser(deleteConfirmUser.id);

      // Başarılı silme sonrası listeyi güncelle
      setUsers((prev) => prev.filter((u) => u.id !== deleteConfirmUser.id));
      closeDeleteConfirm();
    } catch (err) {
      console.error("Kullanıcı silme hatası:", err);
      // Türkçe hata mesajı çevirisi
      const errorMessage = translateError(
        err?.response?.data || err?.message || err,
      );
      alert(errorMessage);
    } finally {
      setDeleting(false);
    }
  };

  // ============================================================================
  // Toplu İşlem (Bulk Actions) Fonksiyonları
  // Çoklu kullanıcı seçimi ve toplu rol/durum değişikliği
  // ============================================================================

  /**
   * Tek kullanıcı seçimi toggle
   */
  const toggleUserSelection = (userId) => {
    setSelectedUserIds((prev) =>
      prev.includes(userId)
        ? prev.filter((id) => id !== userId)
        : [...prev, userId],
    );
  };

  /**
   * Tüm kullanıcıları seç/kaldır (mevcut sayfadaki)
   */
  const toggleSelectAll = () => {
    const selectableUsers = paginatedUsers.filter((u) => canEditUserRole(u));
    const selectableIds = selectableUsers.map((u) => u.id);

    const allSelected = selectableIds.every((id) =>
      selectedUserIds.includes(id),
    );

    if (allSelected) {
      // Tümünü kaldır
      setSelectedUserIds((prev) =>
        prev.filter((id) => !selectableIds.includes(id)),
      );
    } else {
      // Tümünü seç
      setSelectedUserIds((prev) => [...new Set([...prev, ...selectableIds])]);
    }
  };

  /**
   * Toplu rol değiştirme
   */
  const handleBulkRoleChange = async (newRole) => {
    if (selectedUserIds.length === 0) return;

    // SuperAdmin / Admin rolü için yetki kontrolü
    if (
      (newRole === "SuperAdmin" || newRole === "Admin") &&
      currentUser?.role !== "SuperAdmin"
    ) {
      alert(
        "SuperAdmin ve Admin rollerini atamak için SuperAdmin yetkisine sahip olmalısınız.",
      );
      return;
    }

    const targetIds = selectedUserIds.filter((id) => id !== currentUser?.id);
    if (targetIds.length === 0) {
      alert("Kendi rolünüzü değiştiremezsiniz.");
      return;
    }

    const confirmMessage = `${targetIds.length} kullanıcının rolünü "${
      ROLE_DESCRIPTIONS[newRole]?.name || newRole
    }" olarak değiştirmek istediğinizden emin misiniz?`;
    if (!window.confirm(confirmMessage)) return;

    try {
      setBulkActionLoading(true);

      // Paralel olarak tüm kullanıcıların rolünü güncelle
      const results = await Promise.allSettled(
        targetIds.map((userId) =>
          AdminService.updateUserRole(userId, newRole),
        ),
      );

      // Başarılı güncellemeleri say
      const successCount = results.filter(
        (r) => r.status === "fulfilled",
      ).length;
      const failCount = results.filter((r) => r.status === "rejected").length;

      // UI'ı güncelle
      setUsers((prev) =>
        prev.map((u) =>
          targetIds.includes(u.id) ? { ...u, role: newRole } : u,
        ),
      );

      // Seçimleri temizle
      setSelectedUserIds([]);

      // Sonuç bildirimi
      if (failCount > 0) {
        alert(
          `${successCount} kullanıcı güncellendi, ${failCount} kullanıcı güncellenemedi.`,
        );
      } else {
        alert(`${successCount} kullanıcının rolü başarıyla güncellendi.`);
      }
    } catch (err) {
      console.error("Toplu rol güncelleme hatası:", err);
      alert(translateError(err));
    } finally {
      setBulkActionLoading(false);
    }
  };

  /**
   * Toplu aktif/pasif yapma
   */
  const handleBulkStatusChange = async (isActive) => {
    if (selectedUserIds.length === 0) return;

    const statusText = isActive ? "aktif" : "pasif";
    const confirmMessage = `${selectedUserIds.length} kullanıcıyı ${statusText} yapmak istediğinizden emin misiniz?`;
    if (!window.confirm(confirmMessage)) return;

    try {
      setBulkActionLoading(true);

      // Paralel olarak tüm kullanıcıların durumunu güncelle
      const results = await Promise.allSettled(
        selectedUserIds.map(
          (userId) =>
            AdminService.updateUserStatus?.(userId, isActive) ||
            Promise.resolve(),
        ),
      );

      const successCount = results.filter(
        (r) => r.status === "fulfilled",
      ).length;

      // UI'ı güncelle
      setUsers((prev) =>
        prev.map((u) =>
          selectedUserIds.includes(u.id) ? { ...u, isActive } : u,
        ),
      );

      setSelectedUserIds([]);
      alert(`${successCount} kullanıcı ${statusText} yapıldı.`);
    } catch (err) {
      console.error("Toplu durum güncelleme hatası:", err);
      alert(translateError(err));
    } finally {
      setBulkActionLoading(false);
    }
  };

  // ============================================================================
  // Kullanıcı Bilgi Düzenleme İşlemleri
  // Ad, Soyad, Email, Telefon, Adres, Şehir güncelleme
  // Backend: AdminUsersController.UpdateUser endpoint'i (PUT /api/admin/users/{id})
  // ============================================================================

  /**
   * Düzenleme modalını açar ve formu mevcut verilerle doldurur
   */
  const openEditModal = (u) => {
    setEditModalUser(u);
    setEditForm({
      firstName: u.firstName || "",
      lastName: u.lastName || "",
      email: u.email || "",
      phoneNumber: u.phoneNumber || "",
      address: u.address || "",
      city: u.city || "",
    });
    setEditError("");
  };

  const closeEditModal = () => {
    setEditModalUser(null);
    setEditForm({});
    setEditError("");
  };

  const handleEditInputChange = (e) => {
    const { name, value } = e.target;
    setEditForm((prev) => ({ ...prev, [name]: value }));
  };

  /**
   * Kullanıcı bilgilerini günceller
   */
  const handleEditSubmit = async (e) => {
    e.preventDefault();
    if (!editModalUser) return;

    // Zorunlu alan kontrolü
    if (!editForm.firstName?.trim() || !editForm.lastName?.trim() || !editForm.email?.trim()) {
      setEditError("Ad, Soyad ve Email alanları zorunludur.");
      return;
    }

    // Email format validasyonu
    if (!isValidEmail(editForm.email.trim())) {
      setEditError("Geçerli bir email adresi giriniz.");
      return;
    }

    try {
      setEditing(true);
      setEditError("");

      const payload = {
        firstName: editForm.firstName.trim(),
        lastName: editForm.lastName.trim(),
        email: editForm.email.trim(),
        phoneNumber: editForm.phoneNumber?.trim() || null,
        address: editForm.address?.trim() || null,
        city: editForm.city?.trim() || null,
      };

      await AdminService.updateUser(editModalUser.id, payload);

      // UI'ı güncelle
      setUsers((prev) =>
        prev.map((u) =>
          u.id === editModalUser.id
            ? {
                ...u,
                ...payload,
                fullName: `${payload.firstName} ${payload.lastName}`,
              }
            : u,
        ),
      );

      closeEditModal();
    } catch (err) {
      console.error("Kullanıcı güncelleme hatası:", err);
      const errorMessage = translateError(
        err?.response?.data || err?.message || err,
      );
      setEditError(errorMessage);
    } finally {
      setEditing(false);
    }
  };

  // ============================================================================
  // Madde 8: Şifre Güncelleme İşlemleri
  // Admin panelinden kullanıcı şifresi güncelleme
  // ============================================================================

  /**
   * Kullanıcının şifresinin güncellenip güncellenemeyeceğini kontrol eder
   * @param {Object} u - Kontrol edilecek kullanıcı
   * @returns {boolean} - Güncellenebilir ise true
   */
  const canUpdatePassword = (u) => {
    // Güncelleme izni yoksa şifre güncelleyemez
    if (!canUpdateUser) return false;

    // SuperAdmin şifresini sadece SuperAdmin güncelleyebilir
    if (u?.role === "SuperAdmin" && currentUser?.role !== "SuperAdmin") {
      return false;
    }

    return true;
  };

  /**
   * Şifre güncelleme modalını açar
   * @param {Object} u - Şifresi güncellenecek kullanıcı
   */
  const openPasswordModal = (u) => {
    setPasswordModalUser(u);
    setNewPassword("");
    setConfirmPassword("");
    setPasswordError("");
  };

  /**
   * Şifre güncelleme modalını kapatır
   */
  const closePasswordModal = () => {
    setPasswordModalUser(null);
    setNewPassword("");
    setConfirmPassword("");
    setPasswordError("");
  };

  /**
   * Kullanıcı şifresini günceller
   * Backend'e PUT /api/admin/users/{id}/password isteği gönderir
   */
  const handleUpdatePassword = async () => {
    // Validasyonlar
    if (!newPassword.trim()) {
      setPasswordError("Yeni şifre zorunludur.");
      return;
    }

    if (newPassword.length < 6) {
      setPasswordError("Şifre en az 6 karakter olmalıdır.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setPasswordError("Şifreler eşleşmiyor.");
      return;
    }

    try {
      setUpdatingPassword(true);
      setPasswordError("");

      await AdminService.updateUserPassword(passwordModalUser.id, newPassword);

      // Başarılı güncelleme bildirimi
      alert(
        `"${
          passwordModalUser.fullName || passwordModalUser.email
        }" kullanıcısının şifresi başarıyla güncellendi.`,
      );

      closePasswordModal();
    } catch (err) {
      console.error("Şifre güncelleme hatası:", err);
      // Türkçe hata mesajı çevirisi
      const errorMessage = translateError(
        err?.response?.data || err?.message || err,
      );
      setPasswordError(errorMessage);
    } finally {
      setUpdatingPassword(false);
    }
  };

  // ============================================================================
  // Madde 7: Tarih Formatlama Yardımcı Fonksiyonu
  // Kullanıcı listesinde tarihleri okunabilir formatta göstermek için
  // ============================================================================
  const formatDate = (dateString) => {
    if (!dateString) return "-";
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString("tr-TR", {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      });
    } catch {
      return "-";
    }
  };

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ height: "400px" }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="alert alert-danger" role="alert">
        {error}
      </div>
    );
  }

  return (
    <div className="admin-users-page">
      {/* Rol Değişikliği Uyarı Bildirimi */}
      {roleChangeWarning && (
        <div
          className={`alert ${
            roleChangeWarning.type === "self" ? "alert-success" : "alert-info"
          } alert-dismissible fade show mb-3`}
          role="alert"
          style={{ borderRadius: "12px", border: "none", boxShadow: "0 2px 8px rgba(0,0,0,0.08)" }}
        >
          <div className="d-flex align-items-start">
            <i
              className={`fas ${
                roleChangeWarning.type === "self"
                  ? "fa-check-circle"
                  : "fa-info-circle"
              } me-2 mt-1`}
            ></i>
            <div>
              <strong>
                {roleChangeWarning.type === "self"
                  ? "Rolünüz Güncellendi!"
                  : "Rol Güncellendi!"}
              </strong>
              <p className="mb-0 mt-1">{roleChangeWarning.message}</p>
              {roleChangeWarning.type === "other" && (
                <small className="text-muted d-block mt-2">
                  <i className="fas fa-lightbulb me-1"></i>
                  İpucu: Kullanıcı aktif oturumdaysa, izin cache'i 5 dakika
                  sonra otomatik yenilenir veya kullanıcı çıkış yapıp tekrar
                  giriş yaptığında yeni izinler yüklenir.
                </small>
              )}
            </div>
          </div>
          <button
            type="button"
            className="btn-close"
            onClick={() => setRoleChangeWarning(null)}
            aria-label="Kapat"
          ></button>
        </div>
      )}

      {/* Page Header */}
      <div className="admin-users-hero mb-4">
        <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3">
          <div>
            <h2 className="mb-1" style={{ fontWeight: 700, color: "#1e293b" }}>
              <i className="fas fa-users-cog me-2" style={{ color: "#6366f1" }}></i>
              Kullanıcı Yönetimi
            </h2>
            <p className="text-muted mb-0" style={{ fontSize: "0.9rem" }}>
              Sistemdeki tüm kullanıcıları yönetin, roller atayın ve erişimleri düzenleyin
            </p>
          </div>
          <div className="d-flex gap-2 align-items-center">
            <div className="d-flex gap-2">
              <span className="badge bg-light text-dark border" style={{ fontSize: "0.8rem", padding: "8px 12px" }}>
                <i className="fas fa-users me-1" style={{ color: "#6366f1" }}></i>
                {users.length} Kullanıcı
              </span>
              <span className="badge bg-light text-dark border" style={{ fontSize: "0.8rem", padding: "8px 12px" }}>
                <i className="fas fa-user-check me-1" style={{ color: "#22c55e" }}></i>
                {users.filter(u => u.isActive !== false).length} Aktif
              </span>
            </div>
            {canCreateUser && (
              <button
                className="btn btn-primary d-flex align-items-center gap-2"
                onClick={openCreateModal}
                style={{
                  borderRadius: "10px",
                  padding: "10px 20px",
                  fontWeight: 600,
                  background: "linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%)",
                  border: "none",
                  boxShadow: "0 4px 12px rgba(99, 102, 241, 0.3)",
                }}
              >
                <i className="fas fa-user-plus"></i>
                <span className="d-none d-sm-inline">Yeni Kullanıcı</span>
              </button>
            )}
          </div>
        </div>
      </div>

      {/* ====================================================================
          Arama ve Filtreleme Bileşeni
          ==================================================================== */}
      <UserSearchFilter
        onFilterChange={handleFilterChange}
        roles={availableRoles}
        totalCount={users.length}
        filteredCount={filteredUsers.length}
      />

      {/* ====================================================================
          Toplu İşlem Araç Çubuğu
          Seçili kullanıcılar varsa gösterilir
          ==================================================================== */}
      {selectedUserIds.length > 0 && (
        <div className="card border-primary mb-3">
          <div className="card-body py-2 d-flex flex-wrap align-items-center gap-3">
            <span className="badge bg-primary fs-6">
              {selectedUserIds.length} kullanıcı seçildi
            </span>

            <div className="vr d-none d-md-block"></div>

            {/* Toplu Rol Değiştirme */}
            <div className="d-flex align-items-center gap-2">
              <label className="form-label mb-0 small text-muted">Rol:</label>
              <select
                className="form-select form-select-sm"
                style={{ width: "auto" }}
                onChange={(e) =>
                  e.target.value && handleBulkRoleChange(e.target.value)
                }
                disabled={bulkActionLoading}
                defaultValue=""
              >
                <option value="" disabled>
                  Seçin...
                </option>
                {assignableRoles.map((role) => (
                  <option
                    key={role.value}
                    value={role.value}
                    disabled={
                      role.requiresSuperAdmin &&
                      currentUser?.role !== "SuperAdmin"
                    }
                  >
                    {role.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="vr d-none d-md-block"></div>

            {/* Toplu Durum Değiştirme */}
            <div className="btn-group btn-group-sm">
              <button
                className="btn btn-outline-success"
                onClick={() => handleBulkStatusChange(true)}
                disabled={bulkActionLoading}
                title="Seçili kullanıcıları aktif yap"
              >
                <i className="fas fa-check me-1"></i>
                Aktif Yap
              </button>
              <button
                className="btn btn-outline-secondary"
                onClick={() => handleBulkStatusChange(false)}
                disabled={bulkActionLoading}
                title="Seçili kullanıcıları pasif yap"
              >
                <i className="fas fa-ban me-1"></i>
                Pasif Yap
              </button>
            </div>

            <div className="vr d-none d-md-block"></div>

            {/* Seçimi Temizle */}
            <button
              className="btn btn-sm btn-outline-danger"
              onClick={() => setSelectedUserIds([])}
              disabled={bulkActionLoading}
            >
              <i className="fas fa-times me-1"></i>
              Seçimi Temizle
            </button>

            {bulkActionLoading && (
              <div
                className="spinner-border spinner-border-sm text-primary ms-2"
                role="status"
              >
                <span className="visually-hidden">İşleniyor...</span>
              </div>
            )}
          </div>
        </div>
      )}

      <div className="card">
        <div className="card-header d-flex justify-content-between align-items-center">
          <h5 className="card-title mb-0">Kullanıcılar</h5>
          <small className="text-muted">
            {filteredUsers.length !== users.length
              ? `${filteredUsers.length} / ${users.length} kullanıcı`
              : `${users.length} kullanıcı`}
          </small>
        </div>
        <div className="card-body">
          <div className="table-responsive">
            <table
              className="table table-striped align-middle admin-users-table"
              style={{ tableLayout: "fixed" }}
            >
              <thead>
                <tr>
                  {/* Toplu Seçim Checkbox */}
                  <th style={{ width: "40px" }}>
                    <input
                      type="checkbox"
                      className="form-check-input"
                      checked={
                        paginatedUsers.filter((u) => canEditUserRole(u))
                          .length > 0 &&
                        paginatedUsers
                          .filter((u) => canEditUserRole(u))
                          .every((u) => selectedUserIds.includes(u.id))
                      }
                      onChange={toggleSelectAll}
                      title="Tümünü seç/kaldır"
                    />
                  </th>
                  <th style={{ width: "50px" }}>ID</th>
                  <th style={{ width: "150px" }}>Ad Soyad</th>
                  <th style={{ width: "170px" }}>Email</th>
                  <th style={{ width: "120px" }}>Telefon</th>
                  <th style={{ width: "90px" }}>Şehir</th>
                  <th style={{ width: "130px" }}>Rol</th>
                  <th style={{ width: "70px" }}>Durum</th>
                  <th style={{ width: "120px" }}>Kayıt Tarihi</th>
                  <th style={{ width: "120px" }}>Son Giriş</th>
                  <th style={{ width: "180px" }}>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {paginatedUsers.map((u) => {
                  const roleInfo =
                    ROLE_DESCRIPTIONS[u.role] || ROLE_DESCRIPTIONS.User;
                  const isSelected = selectedUserIds.includes(u.id);
                  const canSelect = canEditUserRole(u);

                  return (
                    <tr
                      key={u.id}
                      className={isSelected ? "table-primary" : ""}
                    >
                      {/* Seçim Checkbox */}
                      <td data-label="">
                        <input
                          type="checkbox"
                          className="form-check-input"
                          checked={isSelected}
                          onChange={() => toggleUserSelection(u.id)}
                          disabled={!canSelect}
                          title={canSelect ? "Seç" : "Bu kullanıcı seçilemez"}
                        />
                      </td>
                      <td data-label="ID">{u.id}</td>
                      <td
                        data-label="Ad Soyad"
                        style={{
                          overflow: "hidden",
                          textOverflow: "ellipsis",
                          whiteSpace: "nowrap",
                        }}
                      >
                        {u.fullName ||
                          `${u.firstName ?? ""} ${u.lastName ?? ""}`.trim()}
                      </td>
                      <td
                        data-label="Email"
                        style={{
                          overflow: "hidden",
                          textOverflow: "ellipsis",
                          whiteSpace: "nowrap",
                        }}
                        title={u.email}
                      >
                        {u.email}
                      </td>
                      {/* Telefon numarası — opsiyonel alan */}
                      <td data-label="Telefon">
                        <small>{u.phoneNumber || "-"}</small>
                      </td>
                      {/* Şehir */}
                      <td data-label="Şehir">
                        <small>{u.city || "-"}</small>
                      </td>
                      <td data-label="Rol">
                        <span
                          className={`badge ${
                            roleInfo.color === "purple"
                              ? "text-white"
                              : `bg-${roleInfo.color}`
                          } ${
                            roleInfo.color === "warning" ||
                            roleInfo.color === "light"
                              ? "text-dark"
                              : ""
                          }`}
                          style={
                            roleInfo.color === "purple"
                              ? { backgroundColor: "#9333ea" }
                              : undefined
                          }
                          title={roleInfo.description}
                        >
                          {roleInfo.icon} {roleInfo.name}
                        </span>
                      </td>
                      {/* Madde 7: Aktif/Pasif Durumu */}
                      <td data-label="Durum">
                        <span
                          className={`badge ${
                            u.isActive !== false ? "bg-success" : "bg-secondary"
                          }`}
                        >
                          {u.isActive !== false ? "Aktif" : "Pasif"}
                        </span>
                      </td>
                      {/* Madde 7: Oluşturulma Tarihi */}
                      <td data-label="Kayıt Tarihi">
                        <small className="text-muted">
                          {formatDate(u.createdAt)}
                        </small>
                      </td>
                      {/* Madde 7: Son Giriş Tarihi */}
                      <td data-label="Son Giriş">
                        <small className="text-muted">
                          {u.lastLoginAt
                            ? formatDate(u.lastLoginAt)
                            : "Hiç giriş yapmadı"}
                        </small>
                      </td>
                      <td data-label="İşlemler">
                        <div className="d-flex gap-2 flex-wrap">
                          {/* Kullanıcı Bilgi Düzenleme Butonu */}
                          {canUpdateUser && (
                            <button
                              className="btn btn-sm btn-outline-info admin-users-action-btn"
                              onClick={() => openEditModal(u)}
                              title="Kullanıcı bilgilerini düzenle"
                            >
                              <i className="fas fa-edit me-1"></i>
                              Düzenle
                            </button>
                          )}
                          {canEditUserRole(u) && (
                            <button
                              className="btn btn-sm btn-outline-primary admin-users-action-btn"
                              onClick={() => openRoleModal(u)}
                              title="Kullanıcı rolünü düzenle"
                            >
                              <i className="fas fa-user-edit me-1"></i>
                              Rolü Düzenle
                            </button>
                          )}
                          {/* Madde 8: Şifre Güncelleme Butonu */}
                          {canUpdatePassword(u) && (
                            <button
                              className="btn btn-sm btn-outline-warning admin-users-action-btn"
                              onClick={() => openPasswordModal(u)}
                              title="Kullanıcı şifresini güncelle"
                            >
                              <i className="fas fa-key me-1"></i>
                              Şifre
                            </button>
                          )}
                          {canDeleteUser(u) && (
                            <button
                              className="btn btn-sm btn-outline-danger admin-users-action-btn"
                              onClick={() => openDeleteConfirm(u)}
                              title="Kullanıcıyı sil"
                            >
                              <i className="fas fa-trash-alt me-1"></i>
                              Sil
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Sayfalama Bileşeni */}
          <Pagination
            currentPage={currentPage}
            totalItems={filteredUsers.length}
            itemsPerPage={ITEMS_PER_PAGE}
            onPageChange={setCurrentPage}
          />

          {paginatedUsers.length === 0 && !error && (
            <div className="text-center py-4">
              <i className="fas fa-users fa-3x text-muted mb-3"></i>
              <p className="text-muted">
                {filters.search ||
                filters.role !== "all" ||
                filters.status !== "all"
                  ? "Arama kriterlerine uygun kullanıcı bulunamadı."
                  : "Henüz kullanıcı bulunmuyor."}
              </p>
            </div>
          )}
        </div>
      </div>

      {/* ====================================================================
          Rol Açıklamaları — Tüm Sistemdeki Roller
          8 rol: SuperAdmin, StoreManager, CustomerSupport, Logistics,
                 StoreAttendant, Dispatcher, Courier, User
          ==================================================================== */}
      <div className="card mb-4 mt-4">
        <div className="card-header bg-dark text-white">
          <h5 className="card-title mb-0">
            <i className="fas fa-user-tag me-2"></i>
            Rol Açıklamaları
          </h5>
        </div>
        <div className="card-body">
          <div className="row">
            {/* Süper Yönetici */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-danger">
                <div className="card-header bg-danger text-white">
                  <strong>
                    {ROLE_DESCRIPTIONS.SuperAdmin.icon}{" "}
                    {ROLE_DESCRIPTIONS.SuperAdmin.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.SuperAdmin.description}</small>
                </div>
              </div>
            </div>

            {/* Mağaza Yöneticisi */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-warning">
                <div className="card-header bg-warning text-dark">
                  <strong>
                    {ROLE_DESCRIPTIONS.StoreManager.icon}{" "}
                    {ROLE_DESCRIPTIONS.StoreManager.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.StoreManager.description}</small>
                </div>
              </div>
            </div>

            {/* Müşteri Hizmetleri */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-info">
                <div className="card-header bg-info text-white">
                  <strong>
                    {ROLE_DESCRIPTIONS.CustomerSupport.icon}{" "}
                    {ROLE_DESCRIPTIONS.CustomerSupport.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.CustomerSupport.description}</small>
                </div>
              </div>
            </div>

            {/* Lojistik Görevlisi */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-secondary">
                <div className="card-header bg-secondary text-white">
                  <strong>
                    {ROLE_DESCRIPTIONS.Logistics.icon}{" "}
                    {ROLE_DESCRIPTIONS.Logistics.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.Logistics.description}</small>
                </div>
              </div>
            </div>

            {/* Market Görevlisi — Sipariş hazırlama */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-primary">
                <div className="card-header bg-primary text-white">
                  <strong>
                    {ROLE_DESCRIPTIONS.StoreAttendant.icon}{" "}
                    {ROLE_DESCRIPTIONS.StoreAttendant.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.StoreAttendant.description}</small>
                </div>
              </div>
            </div>

            {/* Sevkiyat Görevlisi — Kurye atama */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-success">
                <div className="card-header bg-success text-white">
                  <strong>
                    {ROLE_DESCRIPTIONS.Dispatcher.icon}{" "}
                    {ROLE_DESCRIPTIONS.Dispatcher.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.Dispatcher.description}</small>
                </div>
              </div>
            </div>

            {/* Kurye — Teslimat */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100" style={{ borderColor: "#9333ea" }}>
                <div
                  className="card-header text-white"
                  style={{ backgroundColor: "#9333ea" }}
                >
                  <strong>
                    {ROLE_DESCRIPTIONS.Courier.icon}{" "}
                    {ROLE_DESCRIPTIONS.Courier.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.Courier.description}</small>
                </div>
              </div>
            </div>

            {/* Müşteri */}
            <div className="col-md-6 col-lg-4 mb-3">
              <div className="card h-100 border-light">
                <div className="card-header bg-light text-dark">
                  <strong>
                    {ROLE_DESCRIPTIONS.User.icon} {ROLE_DESCRIPTIONS.User.name}
                  </strong>
                </div>
                <div className="card-body">
                  <small>{ROLE_DESCRIPTIONS.User.description}</small>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* ====================================================================
          DİNAMİK RBAC İzin Matrisi
          Backend'den çekilen rol-izin verileriyle çalışır.
          Admin checkbox ile izin ekleyip çıkarabilir.
          Mobil uyumlu: yatay scroll + sticky ilk sütun
          ==================================================================== */}
      <div className="card mb-4">
        <div className="card-header bg-primary text-white d-flex justify-content-between align-items-center">
          <h5 className="card-title mb-0">
            <i className="fas fa-shield-alt me-2"></i>
            Rol Bazli Erisim Kontrol (RBAC) Matrisi
          </h5>
          <button
            className="btn btn-sm btn-outline-light"
            onClick={loadMatrixData}
            disabled={matrixLoading}
          >
            <i className={`fas fa-sync-alt ${matrixLoading ? "fa-spin" : ""}`}></i>
            {" "}Yenile
          </button>
        </div>
        <div className="card-body">
          {/* Durum mesajlari */}
          {matrixSaveSuccess && (
            <div className="alert alert-success alert-dismissible fade show py-2" role="alert">
              <i className="fas fa-check-circle me-1"></i> {matrixSaveSuccess}
              <button type="button" className="btn-close btn-sm" onClick={() => setMatrixSaveSuccess(null)}></button>
            </div>
          )}
          {matrixError && (
            <div className="alert alert-danger alert-dismissible fade show py-2" role="alert">
              <i className="fas fa-exclamation-triangle me-1"></i> {matrixError}
              <button type="button" className="btn-close btn-sm" onClick={() => setMatrixError(null)}></button>
            </div>
          )}

          {/* Yukleniyor */}
          {matrixLoading && !matrixData && (
            <div className="text-center py-5">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Yukleniyor...</span>
              </div>
              <p className="mt-2 text-muted">Izin matrisi yukleniyor...</p>
            </div>
          )}

          {/* Matris verisi yoksa */}
          {!matrixLoading && !matrixData && !matrixError && (
            <div className="text-center py-4 text-muted">
              <i className="fas fa-info-circle fa-2x mb-2 d-block"></i>
              Izin matrisi yuklenemedi. Lutfen <strong>Yenile</strong> butonuna tiklayin.
            </div>
          )}

          {/* Dinamik Matris Tablosu */}
          {matrixData && matrixData.permissionHeaders?.length > 0 && (
            <>
              <p className="text-muted mb-3">
                Her rol icin hangi modullere erisim izni oldugunu gosteren tablo.
                {currentUser?.role === "SuperAdmin"
                  ? " Checkbox'lara tiklayarak izinleri duzenleyebilirsiniz."
                  : " (Salt okunur gorunum)"}
              </p>
              <div className="table-responsive permission-matrix-wrapper">
                <table className="table table-bordered table-hover permission-matrix mb-0">
                  <thead className="table-dark">
                    <tr>
                      <th className="pm-sticky-col" style={{ minWidth: "220px" }}>
                        Modul / Islem
                      </th>
                      {matrixData.roleMatrix.map((role) => {
                        const rName = role.RoleName || role.roleName;
                        const rDisplay = role.RoleDisplayName || role.roleDisplayName || rName;
                        const desc = ROLE_DESCRIPTIONS[rName];
                        return (
                          <th key={rName} className="text-center" style={{ minWidth: "90px" }}>
                            <span
                              className={`badge ${
                                rName === "SuperAdmin" ? "bg-danger" :
                                rName === "Admin" ? "bg-dark" :
                                rName === "StoreManager" ? "bg-warning text-dark" :
                                rName === "CustomerSupport" ? "bg-info" :
                                rName === "Logistics" ? "bg-secondary" :
                                rName === "StoreAttendant" ? "bg-primary" :
                                rName === "Dispatcher" ? "bg-success" :
                                rName === "Courier" ? "text-white" :
                                "bg-light text-dark"
                              }`}
                              style={rName === "Courier" ? { backgroundColor: "#9333ea" } : undefined}
                            >
                              {desc?.icon || ""} {rDisplay}
                            </span>
                          </th>
                        );
                      })}
                    </tr>
                  </thead>
                  <tbody>
                    {matrixData.permissionHeaders.map((moduleGroup) => {
                      const moduleName = moduleGroup.Module || moduleGroup.module;
                      const moduleDisplay = moduleGroup.ModuleDisplayName || moduleGroup.moduleDisplayName || moduleName;
                      const perms = moduleGroup.Permissions || moduleGroup.permissions || [];
                      const isExpanded = matrixExpandedModules[moduleName] !== false;

                      return (
                        <React.Fragment key={moduleName}>
                          {/* Modul baslik satiri - tiklayinca ac/kapa */}
                          <tr
                            className="table-light"
                            style={{ cursor: "pointer", userSelect: "none" }}
                            onClick={() => toggleMatrixModule(moduleName)}
                          >
                            <td
                              colSpan={1 + (matrixData.roleMatrix?.length || 0)}
                              className="pm-sticky-col"
                            >
                              <strong>
                                <i className={`fas fa-chevron-${isExpanded ? "down" : "right"} me-2`}></i>
                                {moduleDisplay}
                                <span className="badge bg-secondary ms-2">{perms.length}</span>
                              </strong>
                            </td>
                          </tr>

                          {/* Modul izinleri - sadece aciksa goster */}
                          {isExpanded && perms.map((perm) => {
                            const permId = perm.Id || perm.id;
                            const permDisplay = perm.DisplayName || perm.displayName || perm.Name || perm.name;

                            return (
                              <tr key={permId}>
                                <td className="ps-4 pm-sticky-col" style={{ fontSize: "0.9rem" }}>
                                  {permDisplay}
                                </td>
                                {matrixData.roleMatrix.map((role) => {
                                  const roleId = role.RoleId || role.roleId;
                                  const rName = role.RoleName || role.roleName;
                                  const canEdit = role.CanEdit !== undefined ? role.CanEdit : role.canEdit;
                                  const rolePerms = role.Permissions || role.permissions || [];
                                  const permEntry = rolePerms.find(
                                    (rp) => (rp.PermissionId || rp.permissionId) === permId,
                                  );
                                  const hasPerm = permEntry?.HasPermission ?? permEntry?.hasPermission ?? false;
                                  const savingKey = `${roleId}_${permId}`;
                                  const isSaving = !!matrixSaving[savingKey];
                                  const isSuperAdmin = rName === "SuperAdmin";
                                  const isEditable = canEdit !== false && !isSuperAdmin && currentUser?.role === "SuperAdmin";

                                  return (
                                    <td key={`${roleId}_${permId}`} className="text-center align-middle">
                                      {isSaving ? (
                                        <div className="spinner-border spinner-border-sm text-primary" role="status">
                                          <span className="visually-hidden">...</span>
                                        </div>
                                      ) : isEditable ? (
                                        <div className="form-check d-flex justify-content-center m-0">
                                          <input
                                            className="form-check-input"
                                            type="checkbox"
                                            checked={hasPerm}
                                            onChange={() => handlePermissionToggle(roleId, permId, hasPerm)}
                                            style={{ width: "1.2em", height: "1.2em", cursor: "pointer" }}
                                          />
                                        </div>
                                      ) : (
                                        <span className={hasPerm ? "text-success" : "text-danger"}>
                                          {hasPerm ? (
                                            <i className="fas fa-check-circle"></i>
                                          ) : (
                                            <i className="fas fa-times-circle"></i>
                                          )}
                                        </span>
                                      )}
                                    </td>
                                  );
                                })}
                              </tr>
                            );
                          })}
                        </React.Fragment>
                      );
                    })}
                  </tbody>
                </table>
              </div>
              <div className="mt-3 d-flex flex-wrap gap-3">
                <small className="text-muted">
                  <i className="fas fa-check-circle text-success me-1"></i> Izin var |
                  <i className="fas fa-times-circle text-danger mx-1"></i> Izin yok |
                  <input type="checkbox" className="form-check-input mx-1" disabled readOnly style={{ width: "1em", height: "1em" }} /> Duzenlenebilir
                </small>
                <small className="text-muted">
                  <i className="fas fa-info-circle me-1"></i>
                  SuperAdmin rolunun izinleri degistirilemez (tum izinlere otomatik sahiptir).
                </small>
              </div>
            </>
          )}
        </div>
      </div>

      {selectedUser && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">Rolü Düzenle</h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={closeRoleModal}
                ></button>
              </div>
              <div className="modal-body">
                <p className="mb-2">
                  <strong>Kullanıcı:</strong>{" "}
                  {selectedUser.fullName ||
                    `${selectedUser.firstName ?? ""} ${
                      selectedUser.lastName ?? ""
                    }`.trim()}
                </p>
                <p className="mb-3">
                  <strong>Email:</strong> {selectedUser.email}
                </p>
                <div className="mb-3">
                  <label className="form-label">Rol Seçin</label>
                  <select
                    className="form-select"
                    value={selectedRole}
                    onChange={(e) => setSelectedRole(e.target.value)}
                  >
                    {assignableRoles.map((role) => (
                      <option
                        key={role.value}
                        value={role.value}
                        disabled={
                          role.requiresSuperAdmin &&
                          currentUser?.role !== "SuperAdmin"
                        }
                      >
                        {ROLE_DESCRIPTIONS[role.value]?.icon} {role.label}
                      </option>
                    ))}
                  </select>
                  {ROLE_DESCRIPTIONS[selectedRole] && (
                    <small className="form-text text-muted d-block mt-2">
                      <strong>{ROLE_DESCRIPTIONS[selectedRole].name}:</strong>{" "}
                      {ROLE_DESCRIPTIONS[selectedRole].description}
                    </small>
                  )}
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={closeRoleModal}
                  disabled={saving}
                >
                  İptal
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={handleSaveRole}
                  disabled={saving}
                >
                  {saving ? "Kaydediliyor..." : "Kaydet"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {showCreateModal && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <form onSubmit={handleCreateSubmit}>
                <div className="modal-header">
                  <h5 className="modal-title">Yeni Kullanıcı</h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={closeCreateModal}
                    disabled={creating}
                  ></button>
                </div>
                <div className="modal-body">
                  {createError && (
                    <div className="alert alert-danger">{createError}</div>
                  )}
                  <div className="row g-3">
                    <div className="col-md-6">
                      <label className="form-label">Ad</label>
                      <input
                        type="text"
                        className="form-control"
                        name="firstName"
                        value={createForm.firstName}
                        onChange={handleCreateInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Soyad</label>
                      <input
                        type="text"
                        className="form-control"
                        name="lastName"
                        value={createForm.lastName}
                        onChange={handleCreateInputChange}
                        required
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Email</label>
                      <input
                        type="email"
                        className="form-control"
                        name="email"
                        value={createForm.email}
                        onChange={handleCreateInputChange}
                        placeholder="ornek@domain.com"
                        required
                      />
                      <small className="form-text text-muted">
                        Geçerli bir email adresi giriniz
                      </small>
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Şifre</label>
                      <input
                        type="password"
                        className="form-control"
                        name="password"
                        value={createForm.password}
                        onChange={handleCreateInputChange}
                        minLength={6}
                        placeholder="En az 6 karakter"
                        required
                      />
                      <small className="form-text text-muted">
                        Şifre en az 6 karakter olmalıdır
                      </small>
                    </div>
                    {/* Telefon numarası — opsiyonel, zorunlu değil */}
                    <div className="col-12">
                      <label className="form-label">
                        Telefon Numarası{" "}
                        <small className="text-muted">(opsiyonel)</small>
                      </label>
                      <input
                        type="tel"
                        className="form-control"
                        name="phoneNumber"
                        value={createForm.phoneNumber}
                        onChange={handleCreateInputChange}
                        placeholder="05XX XXX XX XX"
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Adres</label>
                      <input
                        type="text"
                        className="form-control"
                        name="address"
                        value={createForm.address}
                        onChange={handleCreateInputChange}
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Şehir</label>
                      <input
                        type="text"
                        className="form-control"
                        name="city"
                        value={createForm.city}
                        onChange={handleCreateInputChange}
                      />
                    </div>
                    <div className="col-12">
                      <label className="form-label">Rol Seçin</label>
                      <select
                        className="form-select"
                        name="role"
                        value={createForm.role}
                        onChange={handleCreateInputChange}
                      >
                        {assignableRoles.map((role) => (
                          <option
                            key={role.value}
                            value={role.value}
                            disabled={
                              role.requiresSuperAdmin &&
                              currentUser?.role !== "SuperAdmin"
                            }
                          >
                            {ROLE_DESCRIPTIONS[role.value]?.icon} {role.label}
                          </option>
                        ))}
                      </select>
                      {ROLE_DESCRIPTIONS[createForm.role] && (
                        <small className="form-text text-muted d-block mt-2">
                          <strong>
                            {ROLE_DESCRIPTIONS[createForm.role].name}:
                          </strong>{" "}
                          {ROLE_DESCRIPTIONS[createForm.role].description}
                        </small>
                      )}
                    </div>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={closeCreateModal}
                    disabled={creating}
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={creating}
                  >
                    {creating ? "Kaydediliyor..." : "Kullanıcı Ekle"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* ====================================================================
          Kullanıcı Bilgi Düzenleme Modalı
          Ad, Soyad, Email, Telefon, Adres, Şehir güncelleme
          ==================================================================== */}
      {editModalUser && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <form onSubmit={handleEditSubmit}>
                <div className="modal-header bg-info text-white">
                  <h5 className="modal-title">
                    <i className="fas fa-edit me-2"></i>
                    Kullanıcı Düzenle
                  </h5>
                  <button
                    type="button"
                    className="btn-close btn-close-white"
                    onClick={closeEditModal}
                    disabled={editing}
                  ></button>
                </div>
                <div className="modal-body">
                  {editError && (
                    <div className="alert alert-danger">{editError}</div>
                  )}
                  <div className="row g-3">
                    <div className="col-md-6">
                      <label className="form-label">Ad</label>
                      <input
                        type="text"
                        className="form-control"
                        name="firstName"
                        value={editForm.firstName}
                        onChange={handleEditInputChange}
                        required
                        disabled={editing}
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Soyad</label>
                      <input
                        type="text"
                        className="form-control"
                        name="lastName"
                        value={editForm.lastName}
                        onChange={handleEditInputChange}
                        required
                        disabled={editing}
                      />
                    </div>
                    <div className="col-12">
                      <label className="form-label">Email</label>
                      <input
                        type="email"
                        className="form-control"
                        name="email"
                        value={editForm.email}
                        onChange={handleEditInputChange}
                        required
                        disabled={editing}
                      />
                    </div>
                    <div className="col-12">
                      <label className="form-label">
                        Telefon <small className="text-muted">(opsiyonel)</small>
                      </label>
                      <input
                        type="tel"
                        className="form-control"
                        name="phoneNumber"
                        value={editForm.phoneNumber}
                        onChange={handleEditInputChange}
                        placeholder="05XX XXX XX XX"
                        disabled={editing}
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Adres</label>
                      <input
                        type="text"
                        className="form-control"
                        name="address"
                        value={editForm.address}
                        onChange={handleEditInputChange}
                        disabled={editing}
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">Şehir</label>
                      <input
                        type="text"
                        className="form-control"
                        name="city"
                        value={editForm.city}
                        onChange={handleEditInputChange}
                        disabled={editing}
                      />
                    </div>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={closeEditModal}
                    disabled={editing}
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="btn btn-info text-white"
                    disabled={editing}
                  >
                    {editing ? "Kaydediliyor..." : "Kaydet"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* ====================================================================
          Kullanıcı Silme Onay Modalı
          Güvenlik: Geri alınamaz işlem için kullanıcıdan onay alınır
          ==================================================================== */}
      {deleteConfirmUser && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <div className="modal-header bg-danger text-white">
                <h5 className="modal-title">
                  <i className="fas fa-exclamation-triangle me-2"></i>
                  Kullanıcı Silme Onayı
                </h5>
                <button
                  type="button"
                  className="btn-close btn-close-white"
                  onClick={closeDeleteConfirm}
                  disabled={deleting}
                ></button>
              </div>
              <div className="modal-body">
                <div className="alert alert-warning mb-3">
                  <i className="fas fa-warning me-2"></i>
                  <strong>Dikkat!</strong> Bu işlem geri alınamaz.
                </div>
                <p className="mb-2">
                  Aşağıdaki kullanıcıyı silmek istediğinizden emin misiniz?
                </p>
                <div className="card bg-light">
                  <div className="card-body py-2">
                    <p className="mb-1">
                      <strong>Ad Soyad:</strong>{" "}
                      {deleteConfirmUser.fullName ||
                        `${deleteConfirmUser.firstName ?? ""} ${
                          deleteConfirmUser.lastName ?? ""
                        }`.trim()}
                    </p>
                    <p className="mb-1">
                      <strong>Email:</strong> {deleteConfirmUser.email}
                    </p>
                    <p className="mb-0">
                      <strong>Rol:</strong>{" "}
                      <span
                        className={`badge bg-${
                          ROLE_DESCRIPTIONS[deleteConfirmUser.role]?.color ||
                          "secondary"
                        }`}
                      >
                        {ROLE_DESCRIPTIONS[deleteConfirmUser.role]?.icon}{" "}
                        {ROLE_DESCRIPTIONS[deleteConfirmUser.role]?.name ||
                          deleteConfirmUser.role}
                      </span>
                    </p>
                  </div>
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={closeDeleteConfirm}
                  disabled={deleting}
                >
                  <i className="fas fa-times me-1"></i>
                  İptal
                </button>
                <button
                  type="button"
                  className="btn btn-danger"
                  onClick={handleDeleteUser}
                  disabled={deleting}
                >
                  {deleting ? (
                    <>
                      <span
                        className="spinner-border spinner-border-sm me-2"
                        role="status"
                      ></span>
                      Siliniyor...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-trash-alt me-1"></i>
                      Evet, Sil
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ====================================================================
          Madde 8: Şifre Güncelleme Modalı
          Admin panelinden kullanıcı şifresi güncelleme
          ==================================================================== */}
      {passwordModalUser && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog">
            <div className="modal-content">
              <div className="modal-header bg-warning text-dark">
                <h5 className="modal-title">
                  <i className="fas fa-key me-2"></i>
                  Şifre Güncelle
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={closePasswordModal}
                  disabled={updatingPassword}
                ></button>
              </div>
              <div className="modal-body">
                {passwordError && (
                  <div className="alert alert-danger">{passwordError}</div>
                )}

                <div className="card bg-light mb-3">
                  <div className="card-body py-2">
                    <p className="mb-1">
                      <strong>Kullanıcı:</strong>{" "}
                      {passwordModalUser.fullName ||
                        `${passwordModalUser.firstName ?? ""} ${
                          passwordModalUser.lastName ?? ""
                        }`.trim()}
                    </p>
                    <p className="mb-0">
                      <strong>Email:</strong> {passwordModalUser.email}
                    </p>
                  </div>
                </div>

                <div className="mb-3">
                  <label className="form-label">Yeni Şifre</label>
                  <input
                    type="password"
                    className="form-control"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="En az 6 karakter"
                    minLength={6}
                    disabled={updatingPassword}
                  />
                  <small className="form-text text-muted">
                    Şifre en az 6 karakter olmalıdır
                  </small>
                </div>

                <div className="mb-3">
                  <label className="form-label">Şifre Tekrar</label>
                  <input
                    type="password"
                    className="form-control"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    placeholder="Şifreyi tekrar girin"
                    minLength={6}
                    disabled={updatingPassword}
                  />
                </div>

                <div className="alert alert-info mb-0">
                  <i className="fas fa-info-circle me-2"></i>
                  <small>
                    Kullanıcının mevcut şifresi değiştirilecektir. Kullanıcı bir
                    sonraki girişinde yeni şifreyi kullanmalıdır.
                  </small>
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={closePasswordModal}
                  disabled={updatingPassword}
                >
                  <i className="fas fa-times me-1"></i>
                  İptal
                </button>
                <button
                  type="button"
                  className="btn btn-warning"
                  onClick={handleUpdatePassword}
                  disabled={
                    updatingPassword || !newPassword || !confirmPassword
                  }
                >
                  {updatingPassword ? (
                    <>
                      <span
                        className="spinner-border spinner-border-sm me-2"
                        role="status"
                      ></span>
                      Güncelleniyor...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-save me-1"></i>
                      Şifreyi Güncelle
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminUsers;
