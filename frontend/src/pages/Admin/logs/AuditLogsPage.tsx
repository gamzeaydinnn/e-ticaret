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
import { AdminService } from "../../../services/adminService";

const AuditLogsPage = () => {
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
    action: "",
    startDate: "",
    endDate: "",
    search: "",
  });
  const [viewer, setViewer] = useState({
    open: false,
    payload: null,
    mode: "old",
  });

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

  const closeViewer = () =>
    setViewer({ open: false, payload: null, mode: "old" });

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
    <Box sx={{ p: { xs: 1, md: 2 } }}>
      <Typography
        variant="h5"
        sx={{ fontSize: { xs: "1.25rem", md: "1.5rem" }, mb: 2 }}
      >
        Audit Logs
      </Typography>
      <Paper sx={{ p: { xs: 1.5, md: 3 }, mb: 2 }}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={1.5}
          sx={{ flexWrap: "wrap", gap: { xs: 1, md: 2 } }}
        >
          <TextField
            label="Entity Type"
            value={filters.entityType}
            onChange={(e) => handleFilterChange("entityType", e.target.value)}
            size="small"
            sx={{ minWidth: { xs: "100%", sm: 120 }, flex: { sm: 1 } }}
          />
          <TextField
            label="Action"
            value={filters.action}
            onChange={(e) => handleFilterChange("action", e.target.value)}
            size="small"
            sx={{ minWidth: { xs: "100%", sm: 100 }, flex: { sm: 1 } }}
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
                  Aksiyon
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
                  Kullanıcı
                </TableCell>
                <TableCell
                  sx={{
                    display: { xs: "none", lg: "table-cell" },
                    fontSize: "0.75rem",
                  }}
                >
                  Oluşturma
                </TableCell>
                <TableCell sx={{ fontSize: "0.75rem", px: { xs: 1, md: 2 } }}>
                  Değerler
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
                    <Chip
                      label={log.action}
                      size="small"
                      color="warning"
                      sx={{ fontSize: "0.65rem" }}
                    />
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
                        #{log.entityId || "-"}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell sx={{ display: { xs: "none", sm: "table-cell" } }}>
                    <Typography variant="body2" sx={{ fontSize: "0.75rem" }}>
                      {log.performedBy || `User #${log.adminUserId ?? "-"}`}
                    </Typography>
                  </TableCell>
                  <TableCell
                    sx={{
                      display: { xs: "none", lg: "table-cell" },
                      fontSize: "0.7rem",
                    }}
                  >
                    {formatDate(log.createdAt)}
                  </TableCell>
                  <TableCell sx={{ px: { xs: 1, md: 2 } }}>
                    <Stack direction="row" spacing={0.5}>
                      <Button
                        variant="outlined"
                        size="small"
                        onClick={() => openViewer(log, "old")}
                        disabled={!log.oldValues}
                        sx={{
                          minWidth: { xs: 40, md: 60 },
                          fontSize: "0.65rem",
                          px: { xs: 0.5, md: 1 },
                        }}
                      >
                        Eski
                      </Button>
                      <Button
                        variant="contained"
                        size="small"
                        onClick={() => openViewer(log, "new")}
                        disabled={!log.newValues}
                        sx={{
                          minWidth: { xs: 40, md: 60 },
                          fontSize: "0.65rem",
                          px: { xs: 0.5, md: 1 },
                        }}
                      >
                        Yeni
                      </Button>
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
              {!loading && logs.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={6}
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
        open={viewer.open}
        onClose={closeViewer}
        maxWidth="md"
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
          {viewer.mode === "old" ? "Eski Değerler" : "Yeni Değerler"}
        </DialogTitle>
        <DialogContent sx={{ p: { xs: 1.5, md: 3 } }}>
          <pre
            style={{
              whiteSpace: "pre-wrap",
              fontSize: "0.75rem",
              overflow: "auto",
            }}
          >
            {renderJson(
              viewer.mode === "old"
                ? viewer.payload?.oldValues
                : viewer.payload?.newValues
            )}
          </pre>
        </DialogContent>
      </Dialog>
    </Box>
  );
};

export default AuditLogsPage;
