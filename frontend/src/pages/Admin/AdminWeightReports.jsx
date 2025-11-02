import React from "react";
import AdminLayout from "../../components/AdminLayout";
import WeightReportsPanel from "../../admin/WeightReportsPanel";

export default function AdminWeightReports() {
  return (
    <AdminLayout>
      <div className="container-fluid">
        <WeightReportsPanel />
      </div>
    </AdminLayout>
  );
}
