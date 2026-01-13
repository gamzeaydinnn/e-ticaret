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
import { AdminService } from "../../../services/adminService";

const SystemLogsPage = () => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [pagination, setPagination] = useState({
    page: 0,
    pageSize: 20,
    total: 0,
  });
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
    <Box sx={{ p: { xs: 1, md: 2 } }}>
      <Typography
        variant="h5"
        sx={{ fontSize: { xs: "1.25rem", md: "1.5rem" }, mb: 2 }}
      >
        System Logs
      </Typography>
      <Paper sx={{ p: { xs: 1.5, md: 3 }, mb: 2 }}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={1.5}
          sx={{ flexWrap: "wrap", gap: { xs: 1, md: 2 } }}
        >
          <TextField
            label="Entity"
            value={filters.entityType}
            onChange={(e) => handleFilterChange("entityType", e.target.value)}
            size="small"
            sx={{ minWidth: { xs: "100%", sm: 100 }, flex: { sm: 1 } }}
          />
          <TextField
            label="Status"
            value={filters.status}
            onChange={(e) => handleFilterChange("status", e.target.value)}
            size="small"
            sx={{ minWidth: { xs: "48%", sm: 80 }, flex: { sm: 1 } }}
          />
          <TextField
            label="Direction"
            value={filters.direction}
            onChange={(e) => handleFilterChange("direction", e.target.value)}
            size="small"
            sx={{ minWidth: { xs: "48%", sm: 80 }, flex: { sm: 1 } }}
          />
          <TextField
            label="Başlangıç"
            type="date"
            InputLabelProps={{ shrink: true }}
            value={filters.startDate}
            onChange={(e) => handleFilterChange("startDate", e.target.value)}
            size="small"
            sx={{ minWidth: { xs: "48%", sm: 130 } }}
          />
          <TextField
            label="Bitiş"
            type="date"
            InputLabelProps={{ shrink: true }}
            value={filters.endDate}
            onChange={(e) => handleFilterChange("endDate", e.target.value)}
            size="small"
            sx={{ minWidth: { xs: "48%", sm: 130 } }}
          />
          <TextField
            label="Ara"
            value={filters.search}
            onChange={(e) => handleFilterChange("search", e.target.value)}
            size="small"
            sx={{ minWidth: { xs: "100%", sm: 100 }, flex: { sm: 1 } }}
          />
          <Button
            variant="contained"
            onClick={fetchLogs}
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
                  Entity
                </TableCell>
                <TableCell
                  sx={{
                    display: { xs: "none", sm: "table-cell" },
                    fontSize: "0.75rem",
                  }}
                >
                  Direction
                </TableCell>
                <TableCell sx={{ fontSize: "0.75rem", px: { xs: 1, md: 2 } }}>
                  Status
                </TableCell>
                <TableCell
                  sx={{
                    display: { xs: "none", lg: "table-cell" },
                    fontSize: "0.75rem",
                  }}
                >
                  Attempts
                </TableCell>
                <TableCell
                  sx={{
                    display: { xs: "none", md: "table-cell" },
                    fontSize: "0.75rem",
                  }}
                >
                  Mesaj
                </TableCell>
                <TableCell sx={{ fontSize: "0.75rem", px: { xs: 1, md: 2 } }}>
                  Detay
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {logs.map((log) => (
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
                      <Typography variant="body2" sx={{ fontSize: "0.75rem" }}>
                        {log.entityType}
                      </Typography>
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ fontSize: "0.65rem" }}
                      >
                        #{log.internalId || log.externalId || "-"}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell sx={{ display: { xs: "none", sm: "table-cell" } }}>
                    <Chip
                      label={log.direction}
                      size="small"
                      sx={{ fontSize: "0.65rem" }}
                    />
                  </TableCell>
                  <TableCell sx={{ px: { xs: 1, md: 2 } }}>
                    <Chip
                      label={log.status}
                      size="small"
                      color="success"
                      sx={{ fontSize: "0.65rem" }}
                    />
                  </TableCell>
                  <TableCell
                    sx={{
                      display: { xs: "none", lg: "table-cell" },
                      fontSize: "0.75rem",
                    }}
                  >
                    {log.attempts}
                  </TableCell>
                  <TableCell
                    sx={{
                      display: { xs: "none", md: "table-cell" },
                      fontSize: "0.7rem",
                      maxWidth: 150,
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                      whiteSpace: "nowrap",
                    }}
                  >
                    {log.message}
                  </TableCell>
                  <TableCell sx={{ px: { xs: 1, md: 2 } }}>
                    <Button
                      variant="outlined"
                      size="small"
                      onClick={() => setDetailLog(log)}
                      sx={{
                        minWidth: { xs: 50, md: 70 },
                        fontSize: "0.65rem",
                        px: { xs: 0.5, md: 1 },
                      }}
                    >
                      İncele
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {!loading && logs.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={7}
                    align="center"
                    sx={{ fontSize: "0.85rem" }}
                  >
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
          sx={{
            ".MuiTablePagination-selectLabel, .MuiTablePagination-displayedRows":
              {
                fontSize: { xs: "0.7rem", md: "0.875rem" },
              },
          }}
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

      <Dialog
        open={Boolean(detailLog)}
        onClose={() => setDetailLog(null)}
        maxWidth="sm"
        fullWidth
        sx={{
          "& .MuiDialog-paper": {
            m: { xs: 1, md: 3 },
            width: { xs: "calc(100% - 16px)", md: "auto" },
          },
        }}
      >
        <DialogTitle
          sx={{
            fontSize: { xs: "1rem", md: "1.25rem" },
            py: { xs: 1.5, md: 2 },
          }}
        >
          Log Detayı
        </DialogTitle>
        <DialogContent sx={{ p: { xs: 1.5, md: 3 } }}>
          <Stack spacing={1}>
            <Typography variant="body2" sx={{ fontSize: "0.8rem" }}>
              <strong>Status:</strong> {detailLog?.status}
            </Typography>
            <Typography variant="body2" sx={{ fontSize: "0.8rem" }}>
              <strong>Attempts:</strong> {detailLog?.attempts}
            </Typography>
            <Typography variant="body2" sx={{ fontSize: "0.8rem" }}>
              <strong>Son Deneme:</strong>{" "}
              {formatDate(detailLog?.lastAttemptAt)}
            </Typography>
            <Typography variant="body2" sx={{ fontSize: "0.8rem" }}>
              <strong>Oluşturma:</strong> {formatDate(detailLog?.createdAt)}
            </Typography>
            <Typography variant="body2" sx={{ fontSize: "0.8rem" }}>
              <strong>Mesaj:</strong> {detailLog?.message}
            </Typography>
            <Typography variant="body2" sx={{ fontSize: "0.8rem" }}>
              <strong>Son Hata:</strong>
            </Typography>
            <pre
              style={{
                whiteSpace: "pre-wrap",
                fontSize: "0.7rem",
                overflow: "auto",
                maxHeight: 200,
              }}
            >
              {detailLog?.lastError || "(boş)"}
            </pre>
          </Stack>
        </DialogContent>
      </Dialog>
    </Box>
  );
};

export default SystemLogsPage;
