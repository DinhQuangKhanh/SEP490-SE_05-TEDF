import { NotFoundPage, AccessDeniedPage } from "@/pages/errors";
import { useEffect } from "react";
import { Routes, Route, Navigate } from "react-router-dom";
import { AnimatePresence } from "framer-motion";
import { AuthProvider, useAuth } from "@/contexts/AuthContext";
import { MaintenanceProvider } from "@/contexts/MaintenanceContext";
import { SystemErrorProvider } from "@/contexts/SystemErrorContext";
import { BrandingProvider } from "@/contexts/SettingsContext";
import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminLayout, LecturerLayout, StudentLayout } from "@/components/layout";
import {
  LoginPage,
  DashboardPage,
  SettingsPage,
  SemestersPage,
  UsersPage,
  ProjectsPage,
  SupportPage,
  LecturerModerationPage,
  LecturerHistoryPage,
  LecturerReviewPage,
  LecturerGroupsPage,
  LecturerRepositoryPage,
  LecturerGroupDetailPage,
  LecturerSupportPage,
  StudentDashboardPage,
  StudentTopicsPage,
  StudentMyTopicPage,
  StudentSupportPage,
  StudentGroupPage,
  MaintenancePage,
  DepartmentHeadDashboardPage,
  AssignEvaluatorsPage,
  ActivityLogsPage,
  TopicCreatePage,
} from "@/pages";

// Helper function to adjust color brightness
const adjustColor = (color: string, amount: number) => {
  const hex = color.replace("#", "");
  const r = Math.max(0, Math.min(255, parseInt(hex.slice(0, 2), 16) + amount));
  const g = Math.max(0, Math.min(255, parseInt(hex.slice(2, 4), 16) + amount));
  const b = Math.max(0, Math.min(255, parseInt(hex.slice(4, 6), 16) + amount));
  return `#${r.toString(16).padStart(2, "0")}${g.toString(16).padStart(2, "0")}${b.toString(16).padStart(2, "0")}`;
};

const roleHomeMap: Record<string, string> = {
  admin: "/admin",
  mentor: "/lecturer",
  evaluator: "/lecturer",
  student: "/student",
  departmenthead: "/lecturer",
};

/** Redirects authenticated users to their role-based home page, or to /login if not logged in. */
function RoleBasedRedirect() {
  const { user, activeRole, isAuthenticated, isLoading } = useAuth();

  if (isLoading) return null;

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace />;
  }

  return <Navigate to={roleHomeMap[activeRole || user.role] ?? "/login"} replace />;
}

function App() {
  // Apply saved theme color on app initialization
  useEffect(() => {
    const savedColor = localStorage.getItem("themeColor") || "#2c6090";
    document.documentElement.style.setProperty("--color-primary", savedColor);
    document.documentElement.style.setProperty("--color-primary-dark", adjustColor(savedColor, -20));
    document.documentElement.style.setProperty("--color-primary-light", adjustColor(savedColor, 20));
  }, []);
  return (
    <MaintenanceProvider>
      <AuthProvider>
        <SystemErrorProvider>
          <BrandingProvider>
          <AnimatePresence mode="wait">
            <Routes>
              {/* Public Routes */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/maintenance" element={<MaintenancePage />} />
              <Route path="/403" element={<AccessDeniedPage />} />

              {/* Protected Admin Routes */}
              <Route
                path="/admin"
                element={
                  <ProtectedRoute allowedRoles={["admin"]}>
                    <AdminLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<DashboardPage />} />
                <Route path="settings" element={<SettingsPage />} />
                <Route path="semesters" element={<SemestersPage />} />
                <Route path="users" element={<UsersPage />} />
                <Route path="projects" element={<ProjectsPage />} />
                <Route path="activity-logs" element={<ActivityLogsPage />} />
                <Route path="support" element={<SupportPage />} />
              </Route>

              {/* Protected Lecturer Routes (Mentor + Evaluator + DepartmentHead, unified) */}
              <Route
                path="/lecturer"
                element={
                  <ProtectedRoute allowedRoles={["mentor", "evaluator", "departmenthead"]}>
                    <LecturerLayout />
                  </ProtectedRoute>
                }
              >
                {/* Research topic repository (own topics; all topics for DepartmentHead) */}
                <Route index element={<LecturerRepositoryPage />} />
                <Route path="groups" element={<LecturerGroupsPage />} />
                <Route path="groups/:id" element={<LecturerGroupDetailPage />} />
                <Route path="create" element={<TopicCreatePage />} />
                <Route path="moderate" element={<LecturerModerationPage />} />
                <Route path="moderate/:id" element={<LecturerReviewPage />} />
                <Route path="history" element={<LecturerHistoryPage />} />
                <Route path="support" element={<LecturerSupportPage />} />
                {/* Department-Head-only pages */}
                <Route
                  path="dashboard"
                  element={
                    <ProtectedRoute allowedRoles={["departmenthead"]}>
                      <DepartmentHeadDashboardPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="assign"
                  element={
                    <ProtectedRoute allowedRoles={["departmenthead"]}>
                      <AssignEvaluatorsPage />
                    </ProtectedRoute>
                  }
                />
              </Route>

              {/* Protected Student Routes */}
              <Route
                path="/student"
                element={
                  <ProtectedRoute allowedRoles={["student"]}>
                    <StudentLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<StudentDashboardPage />} />
                <Route path="my-topic" element={<StudentMyTopicPage />} />
                <Route path="topics" element={<StudentTopicsPage />} />
                <Route path="groups" element={<StudentGroupPage />} />
                <Route path="support" element={<StudentSupportPage />} />
              </Route>

              {/* Smart redirect: root goes to role-based home */}
              <Route path="/" element={<RoleBasedRedirect />} />

              {/* 404 — any unmatched route */}
              <Route path="*" element={<NotFoundPage />} />
            </Routes>
          </AnimatePresence>
          </BrandingProvider>
        </SystemErrorProvider>
      </AuthProvider>
    </MaintenanceProvider>
  );
}

export default App;
