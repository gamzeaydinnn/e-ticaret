// @ts-nocheck
import React, { useState, useEffect, useCallback } from "react";
import {
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
  Box,
} from "@mui/material";
import AdminLayout from "../../../components/AdminLayout";
import { AdminService } from "../../../services/adminService";

const SystemLogsPage = () => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [pagination, setPagination] = useState({ page: 0, pageSize: 20, total: 0 });
  const [filters, setFilters] = useState({
    entityType: "",
    status: "",
    direction: "",
    startDate: "",
    endDate: "",
    search: "",
  });
  const [detailLog, setDetailLog] = useState(null);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const params = {
        skip: pagination.page * pagination.pageSize,
        take: pagination.pageSize,
        entityType: filters.entityType || undefined,
        status: filters.status || undefined,
        direction: filters.direction || undefined,
        startDate: filters.startDate || undefined,
        endDate: filters.endDate || undefined,
        search: filters.search || undefined,
      };
      const response = await AdminService.getSystemLogs(params);
      const payload = response?.data || response;
      setLogs(payload?.items || []);
      setPagination((prev) => ({ ...prev, total: payload?.total || 0 }));
    } catch (err) {
      console.error("System log fetch error", err);
      setError("Sistem logları getirilemedi");
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

  const formatDate = (value) =>
    value ? new Date(value).toLocaleString("tr-TR") : "-";

  return (
    <AdminLayout>
      <Typography variant="h4" gutterBottom>
        System Logs
      </Typography>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <TextField
            label="Entity"
            value={filters.entityType}
            onChange={(e) => handleFilterChange("entityType", e.target.value)}
            size="small"
          />
          <TextField
            label="Status"
            value={filters.status}
            onChange={(e) => handleFilterChange("status", e.target.value)}
            size="small"
          />
          <TextField
            label="Direction"
            value={filters.direction}
            onChange={(e) => handleFilterChange("direction", e.target.value)}
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
                <TableCell>Entity</TableCell>
                <TableCell>Direction</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Attempts</TableCell>
                <TableCell>Mesaj</TableCell>
                <TableCell>Detay</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {logs.map((log) => (
                <TableRow key={log.id} hover>
                  <TableCell>{log.id}</TableCell>
                  <TableCell>
                    <Stack spacing={0.5}>
                      <Typography variant="body2">{log.entityType}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        #{log.internalId || log.externalId || "-"}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <Chip label={log.direction} size="small" />
                  </TableCell>
                  <TableCell>
                    <Chip label={log.status} size="small" color="success" />
                  </TableCell>
                  <TableCell>{log.attempts}</TableCell>
                  <TableCell>{log.message}</TableCell>
                  <TableCell>
                    <Button variant="outlined" size="small" onClick={() => setDetailLog(log)}>
                      İncele
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {!loading && logs.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} align="center">
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

      <Dialog open={Boolean(detailLog)} onClose={() => setDetailLog(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Log Detayı</DialogTitle>
        <DialogContent>
          <Stack spacing={1}>
            <Typography variant="body2">
              <strong>Status:</strong> {detailLog?.status}
            </Typography>
            <Typography variant="body2">
              <strong>Attempts:</strong> {detailLog?.attempts}
            </Typography>
            <Typography variant="body2">
              <strong>Son Deneme:</strong> {formatDate(detailLog?.lastAttemptAt)}
            </Typography>
            <Typography variant="body2">
              <strong>Oluşturma:</strong> {formatDate(detailLog?.createdAt)}
            </Typography>
            <Typography variant="body2">
              <strong>Mesaj:</strong> {detailLog?.message}
            </Typography>
            <Typography variant="body2">
              <strong>Son Hata:</strong>
            </Typography>
            <pre style={{ whiteSpace: "pre-wrap" }}>{detailLog?.lastError || "(boş)"}</pre>
          </Stack>
        </DialogContent>
      </Dialog>
    </AdminLayout>
  );
};

export default SystemLogsPage;
