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
} from "@mui/material";
import AdminLayout from "../../../components/AdminLayout";
import { AdminService } from "../../../services/adminService";

const InventoryLogsPage = () => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [pagination, setPagination] = useState({
    page: 0,
    pageSize: 20,
    total: 0,
  });
  const [filters, setFilters] = useState({
    productId: "",
    action: "",
    startDate: "",
    endDate: "",
    search: "",
  });

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const params = {
        skip: pagination.page * pagination.pageSize,
        take: pagination.pageSize,
        productId: filters.productId ? Number(filters.productId) : undefined,
        action: filters.action || undefined,
        startDate: filters.startDate || undefined,
        endDate: filters.endDate || undefined,
        search: filters.search || undefined,
      };
      const response = await AdminService.getInventoryLogs(params);
      const payload = response?.data || response;
      setLogs(payload?.items || []);
      setPagination((prev) => ({ ...prev, total: payload?.total || 0 }));
    } catch (err) {
      console.error("Inventory log fetch error", err);
      setError("Kayıtlar yüklenirken bir hata oluştu.");
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

  const renderStockChange = (log) => (
    <Stack spacing={0.5}>
      <Typography variant="body2">Eski: {log.oldStock}</Typography>
      <Typography variant="body2">Yeni: {log.newStock}</Typography>
    </Stack>
  );

  return (
    <AdminLayout>
      <Typography variant="h4" gutterBottom>
        Inventory Logs
      </Typography>

      <Paper sx={{ p: 3, mb: 3 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <TextField
            label="Ürün ID"
            type="number"
            size="small"
            value={filters.productId}
            onChange={(e) => handleFilterChange("productId", e.target.value)}
          />
          <TextField
            label="Aksiyon"
            size="small"
            value={filters.action}
            onChange={(e) => handleFilterChange("action", e.target.value)}
          />
          <TextField
            label="Başlangıç"
            type="date"
            size="small"
            InputLabelProps={{ shrink: true }}
            value={filters.startDate}
            onChange={(e) => handleFilterChange("startDate", e.target.value)}
          />
          <TextField
            label="Bitiş"
            type="date"
            size="small"
            InputLabelProps={{ shrink: true }}
            value={filters.endDate}
            onChange={(e) => handleFilterChange("endDate", e.target.value)}
          />
          <TextField
            label="Ara"
            size="small"
            value={filters.search}
            onChange={(e) => handleFilterChange("search", e.target.value)}
          />
          <Button
            variant="contained"
            onClick={fetchLogs}
            disabled={loading}
            sx={{ whiteSpace: "nowrap" }}
          >
            Filtreyi Uygula
          </Button>
        </Stack>
      </Paper>

      {error && (
        <Box mb={2}>
          <Typography color="error">{error}</Typography>
        </Box>
      )}

      <Paper>
        <TableContainer>
          <Table stickyHeader size="small">
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Ürün</TableCell>
                <TableCell>Aksiyon</TableCell>
                <TableCell>Miktar</TableCell>
                <TableCell>Stok</TableCell>
                <TableCell>Referans</TableCell>
                <TableCell>Oluşturma</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {logs.length === 0 && !loading ? (
                <TableRow>
                  <TableCell colSpan={7} align="center">
                    Kayıt bulunamadı
                  </TableCell>
                </TableRow>
              ) : (
                logs.map((log) => (
                  <TableRow key={log.id} hover>
                    <TableCell>{log.id}</TableCell>
                    <TableCell>
                      <Stack spacing={0.5}>
                        <Typography variant="body2">
                          #{log.productId}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {log.productName || "-"}
                        </Typography>
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Chip label={log.action} size="small" color="primary" />
                    </TableCell>
                    <TableCell>{log.quantity}</TableCell>
                    <TableCell>{renderStockChange(log)}</TableCell>
                    <TableCell>{log.referenceId || "-"}</TableCell>
                    <TableCell>{formatDate(log.createdAt)}</TableCell>
                  </TableRow>
                ))
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
          rowsPerPageOptions={[10, 20, 50, 100]}
        />
      </Paper>
    </AdminLayout>
  );
};

export default InventoryLogsPage;
