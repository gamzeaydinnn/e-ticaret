// @ts-nocheck
import React, { useState, useEffect, useCallback } from "react";
import {
  Box,
  Paper,
  Stack,
  TextField,
  Button,
  Typography,
  TableContainer,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  TablePagination,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
} from "@mui/material";
import AdminLayout from "../../../components/AdminLayout";
import { AdminService } from "../../../services/adminService";

const AuditLogsPage = () => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [pagination, setPagination] = useState({ page: 0, pageSize: 20, total: 0 });
  const [filters, setFilters] = useState({
    entityType: "",
    action: "",
    startDate: "",
    endDate: "",
    search: "",
  });
  const [viewer, setViewer] = useState({ open: false, payload: null, mode: "old" });

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const params = {
        skip: pagination.page * pagination.pageSize,
        take: pagination.pageSize,
        entityType: filters.entityType || undefined,
        action: filters.action || undefined,
        startDate: filters.startDate || undefined,
        endDate: filters.endDate || undefined,
        search: filters.search || undefined,
      };
      const response = await AdminService.getAuditLogs(params);
      const payload = response?.data || response;
      setLogs(payload?.items || []);
      setPagination((prev) => ({ ...prev, total: payload?.total || 0 }));
    } catch (err) {
      console.error("Audit log fetch error", err);
      setError("Kayıtlar yüklenirken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  }, [pagination.page, pagination.pageSize, filters]);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  const handleFilterChange = (field, value) => {
    setFilters((prev) => ({ ...prev, [field]: value }));
    setPagination((prev) => ({ ...prev, page: 0 }));
  };

  const handlePageChange = (_evt, newPage) => {
    setPagination((prev) => ({ ...prev, page: newPage }));
  };

  const handleRowsPerPage = (event) => {
    const newSize = parseInt(event.target.value, 10);
    setPagination((prev) => ({ ...prev, page: 0, pageSize: newSize }));
  };

  const openViewer = (log, mode) => {
    setViewer({ open: true, payload: log, mode });
  };

  const closeViewer = () => setViewer({ open: false, payload: null, mode: "old" });

  const renderJson = (value) => {
    if (!value) return "(boş)";
    try {
      const parsed = typeof value === "string" ? JSON.parse(value) : value;
      return JSON.stringify(parsed, null, 2);
    } catch (err) {
      return value;
    }
  };

  const formatDate = (value) =>
    value ? new Date(value).toLocaleString("tr-TR") : "-";

  return (
    <AdminLayout>
      <Typography variant="h4" gutterBottom>
        Audit Logs
      </Typography>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <TextField
            label="Entity Type"
            value={filters.entityType}
            onChange={(e) => handleFilterChange("entityType", e.target.value)}
            size="small"
          />
          <TextField
            label="Action"
            value={filters.action}
            onChange={(e) => handleFilterChange("action", e.target.value)}
            size="small"
          />
          <TextField
            label="Başlangıç"
            type="date"
            InputLabelProps={{ shrink: true }}
            value={filters.startDate}
            onChange={(e) => handleFilterChange("startDate", e.target.value)}
            size="small"
          />
          <TextField
            label="Bitiş"
            type="date"
            InputLabelProps={{ shrink: true }}
            value={filters.endDate}
            onChange={(e) => handleFilterChange("endDate", e.target.value)}
            size="small"
          />
          <TextField
            label="Ara"
            value={filters.search}
            onChange={(e) => handleFilterChange("search", e.target.value)}
            size="small"
          />
          <Button variant="contained" onClick={fetchLogs} sx={{ whiteSpace: "nowrap" }}>
            Filtreyi Uygula
          </Button>
        </Stack>
      </Paper>

      <Paper>
        <TableContainer>
          <Table stickyHeader size="small">
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Aksiyon</TableCell>
                <TableCell>Entity</TableCell>
                <TableCell>Kullanıcı</TableCell>
                <TableCell>Oluşturma</TableCell>
                <TableCell>Değerler</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {logs.map((log) => (
                <TableRow key={log.id} hover>
                  <TableCell>{log.id}</TableCell>
                  <TableCell>
                    <Chip label={log.action} size="small" color="warning" />
                  </TableCell>
                  <TableCell>
                    <Stack spacing={0.5}>
                      <Typography variant="body2">{log.entityType}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        #{log.entityId || "-"}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {log.performedBy || `User #${log.adminUserId ?? "-"}`}
                    </Typography>
                  </TableCell>
                  <TableCell>{formatDate(log.createdAt)}</TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={1}>
                      <Button
                        variant="outlined"
                        size="small"
                        onClick={() => openViewer(log, "old")}
                        disabled={!log.oldValues}
                      >
                        Eski
                      </Button>
                      <Button
                        variant="contained"
                        size="small"
                        onClick={() => openViewer(log, "new")}
                        disabled={!log.newValues}
                      >
                        Yeni
                      </Button>
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
              {!loading && logs.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center">
                    Kayıt bulunamadı.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          component="div"
          count={pagination.total}
          page={pagination.page}
          onPageChange={handlePageChange}
          rowsPerPage={pagination.pageSize}
          onRowsPerPageChange={handleRowsPerPage}
          rowsPerPageOptions={[10, 20, 50]}
        />
      </Paper>

      {error && (
        <Box mt={2}>
          <Typography color="error">{error}</Typography>
        </Box>
      )}
      {loading && (
        <Box mt={2}>
          <Typography variant="body2">Yükleniyor...</Typography>
        </Box>
      )}

      <Dialog open={viewer.open} onClose={closeViewer} maxWidth="md" fullWidth>
        <DialogTitle>
          {viewer.mode === "old" ? "Eski Değerler" : "Yeni Değerler"}
        </DialogTitle>
        <DialogContent>
          <pre style={{ whiteSpace: "pre-wrap" }}>
            {renderJson(
              viewer.mode === "old"
                ? viewer.payload?.oldValues
                : viewer.payload?.newValues
            )}
          </pre>
        </DialogContent>
      </Dialog>
    </AdminLayout>
  );
};

export default AuditLogsPage;
