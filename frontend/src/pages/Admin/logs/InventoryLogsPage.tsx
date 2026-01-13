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
    <Stack spacing={0.25}>
      <Typography variant="body2" sx={{ fontSize: "0.7rem" }}>
        Eski: {log.oldStock}
      </Typography>
      <Typography variant="body2" sx={{ fontSize: "0.7rem" }}>
        Yeni: {log.newStock}
      </Typography>
    </Stack>
  );

  return (
    <Box sx={{ p: { xs: 1, md: 2 } }}>
      <Typography
        variant="h5"
        sx={{ fontSize: { xs: "1.25rem", md: "1.5rem" }, mb: 2 }}
      >
        Inventory Logs
      </Typography>

      <Paper sx={{ p: { xs: 1.5, md: 3 }, mb: 2 }}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={1.5}
          sx={{ flexWrap: "wrap", gap: { xs: 1, md: 2 } }}
        >
          <TextField
            label="Ürün ID"
            type="number"
            size="small"
            value={filters.productId}
            onChange={(e) => handleFilterChange("productId", e.target.value)}
            sx={{ minWidth: { xs: "48%", sm: 100 }, flex: { sm: 1 } }}
          />
          <TextField
            label="Aksiyon"
            size="small"
            value={filters.action}
            onChange={(e) => handleFilterChange("action", e.target.value)}
            sx={{ minWidth: { xs: "48%", sm: 100 }, flex: { sm: 1 } }}
          />
          <TextField
            label="Başlangıç"
            type="date"
            size="small"
            InputLabelProps={{ shrink: true }}
            value={filters.startDate}
            onChange={(e) => handleFilterChange("startDate", e.target.value)}
            sx={{ minWidth: { xs: "48%", sm: 130 } }}
          />
          <TextField
            label="Bitiş"
            type="date"
            size="small"
            InputLabelProps={{ shrink: true }}
            value={filters.endDate}
            onChange={(e) => handleFilterChange("endDate", e.target.value)}
            sx={{ minWidth: { xs: "48%", sm: 130 } }}
          />
          <TextField
            label="Ara"
            size="small"
            value={filters.search}
            onChange={(e) => handleFilterChange("search", e.target.value)}
            sx={{ minWidth: { xs: "100%", sm: 100 }, flex: { sm: 1 } }}
          />
          <Button
            variant="contained"
            onClick={fetchLogs}
            disabled={loading}
            sx={{
              whiteSpace: "nowrap",
              minHeight: 44,
              width: { xs: "100%", sm: "auto" },
            }}
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
        <TableContainer sx={{ maxHeight: { xs: 400, md: 600 } }}>
          <Table stickyHeader size="small">
            <TableHead>
              <TableRow>
                <TableCell
                  sx={{
                    display: { xs: "none", md: "table-cell" },
                    fontSize: "0.75rem",
                  }}
                >
                  ID
                </TableCell>
                <TableCell sx={{ fontSize: "0.75rem", px: { xs: 1, md: 2 } }}>
                  Ürün
                </TableCell>
                <TableCell sx={{ fontSize: "0.75rem", px: { xs: 1, md: 2 } }}>
                  Aksiyon
                </TableCell>
                <TableCell
                  sx={{
                    display: { xs: "none", sm: "table-cell" },
                    fontSize: "0.75rem",
                  }}
                >
                  Miktar
                </TableCell>
                <TableCell sx={{ fontSize: "0.75rem", px: { xs: 1, md: 2 } }}>
                  Stok
                </TableCell>
                <TableCell
                  sx={{
                    display: { xs: "none", lg: "table-cell" },
                    fontSize: "0.75rem",
                  }}
                >
                  Referans
                </TableCell>
                <TableCell
                  sx={{
                    display: { xs: "none", md: "table-cell" },
                    fontSize: "0.75rem",
                  }}
                >
                  Oluşturma
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {logs.length === 0 && !loading ? (
                <TableRow>
                  <TableCell
                    colSpan={7}
                    align="center"
                    sx={{ fontSize: "0.85rem" }}
                  >
                    Kayıt bulunamadı
                  </TableCell>
                </TableRow>
              ) : (
                logs.map((log) => (
                  <TableRow key={log.id} hover>
                    <TableCell
                      sx={{
                        display: { xs: "none", md: "table-cell" },
                        fontSize: "0.75rem",
                      }}
                    >
                      {log.id}
                    </TableCell>
                    <TableCell sx={{ px: { xs: 1, md: 2 } }}>
                      <Stack spacing={0.25}>
                        <Typography
                          variant="body2"
                          sx={{ fontSize: "0.75rem" }}
                        >
                          #{log.productId}
                        </Typography>
                        <Typography
                          variant="caption"
                          color="text.secondary"
                          sx={{ fontSize: "0.65rem" }}
                        >
                          {log.productName || "-"}
                        </Typography>
                      </Stack>
                    </TableCell>
                    <TableCell sx={{ px: { xs: 1, md: 2 } }}>
                      <Chip
                        label={log.action}
                        size="small"
                        color="primary"
                        sx={{ fontSize: "0.65rem" }}
                      />
                    </TableCell>
                    <TableCell
                      sx={{
                        display: { xs: "none", sm: "table-cell" },
                        fontSize: "0.75rem",
                      }}
                    >
                      {log.quantity}
                    </TableCell>
                    <TableCell sx={{ px: { xs: 1, md: 2 } }}>
                      {renderStockChange(log)}
                    </TableCell>
                    <TableCell
                      sx={{
                        display: { xs: "none", lg: "table-cell" },
                        fontSize: "0.7rem",
                      }}
                    >
                      {log.referenceId || "-"}
                    </TableCell>
                    <TableCell
                      sx={{
                        display: { xs: "none", md: "table-cell" },
                        fontSize: "0.7rem",
                      }}
                    >
                      {formatDate(log.createdAt)}
                    </TableCell>
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
          sx={{
            ".MuiTablePagination-selectLabel, .MuiTablePagination-displayedRows":
              {
                fontSize: { xs: "0.7rem", md: "0.875rem" },
              },
          }}
        />
      </Paper>
    </Box>
  );
};

export default InventoryLogsPage;
