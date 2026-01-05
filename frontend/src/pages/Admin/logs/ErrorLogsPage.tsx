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
  Dialog,
  DialogTitle,
  DialogContent,
  Box,
} from "@mui/material";
import { AdminService } from "../../../services/adminService";

const ErrorLogsPage = () => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [pagination, setPagination] = useState({
    page: 0,
    pageSize: 20,
    total: 0,
  });
  const [filters, setFilters] = useState({
    path: "",
    method: "",
    startDate: "",
    endDate: "",
    search: "",
  });
  const [selectedLog, setSelectedLog] = useState(null);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const params = {
        skip: pagination.page * pagination.pageSize,
        take: pagination.pageSize,
        path: filters.path || undefined,
        method: filters.method || undefined,
        startDate: filters.startDate || undefined,
        endDate: filters.endDate || undefined,
        search: filters.search || undefined,
      };
      const response = await AdminService.getErrorLogs(params);
      const payload = response?.data || response;
      setLogs(payload?.items || []);
      setPagination((prev) => ({ ...prev, total: payload?.total || 0 }));
    } catch (err) {
      console.error("Error log fetch error", err);
      setError("Error log kayıtları alınamadı");
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
    <Box>
      <Typography variant="h4" gutterBottom>
        Error Logs
      </Typography>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <TextField
            label="Path"
            value={filters.path}
            onChange={(e) => handleFilterChange("path", e.target.value)}
            size="small"
          />
          <TextField
            label="Method"
            value={filters.method}
            onChange={(e) => handleFilterChange("method", e.target.value)}
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
          <Button
            variant="contained"
            onClick={fetchLogs}
            sx={{ whiteSpace: "nowrap" }}
          >
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
                <TableCell>Mesaj</TableCell>
                <TableCell>Path</TableCell>
                <TableCell>Method</TableCell>
                <TableCell>User</TableCell>
                <TableCell>Tarih</TableCell>
                <TableCell>Stack Trace</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {logs.map((log) => (
                <TableRow key={log.id} hover>
                  <TableCell>{log.id}</TableCell>
                  <TableCell>{log.message}</TableCell>
                  <TableCell>{log.path || "-"}</TableCell>
                  <TableCell>{log.method || "-"}</TableCell>
                  <TableCell>{log.userId ?? "-"}</TableCell>
                  <TableCell>{formatDate(log.createdAt)}</TableCell>
                  <TableCell>
                    <Button
                      variant="outlined"
                      size="small"
                      onClick={() => setSelectedLog(log)}
                      disabled={!log.stackTrace}
                    >
                      Göster
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

      <Dialog
        open={Boolean(selectedLog)}
        onClose={() => setSelectedLog(null)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Stack Trace</DialogTitle>
        <DialogContent>
          <pre style={{ whiteSpace: "pre-wrap" }}>
            {selectedLog?.stackTrace || "(boş)"}
          </pre>
        </DialogContent>
      </Dialog>
    </Box>
  );
};

export default ErrorLogsPage;
