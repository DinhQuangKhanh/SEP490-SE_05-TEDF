import { NotFoundPage, AccessDeniedPage } from "@/pages/errors";
import { useEffect, type ReactNode } from "react";
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
  IneligiblePage,
  SettingsPage,
  SemestersPage,
  SemesterRosterPage,
  UsersPage,
  SupportPage,
  LecturerModerationPage,
  LecturerHistoryPage,
  LecturerReviewPage,
  LecturerGroupsPage,
  LecturerRepositoryPage,
  LecturerGroupDetailPage,
  LecturerSupportPage,
  SupervisedProjectsPage,
  StudentDashboardPage,
  StudentTopicsPage,
  StudentMyTopicPage,
  StudentSupportPage,
  StudentGroupPage,
  MaintenancePage,
  DepartmentHeadDashboardPage,
  AssignEvaluatorsPage,
  ChecklistConfigPage,
  ActivityLogsPage,
  TopicCreatePage,
  ProfilePage,
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

/** When the signed-in account is blocked by the server gate, show the ineligible screen instead of the app. */
function AccessGate({ children }: Readonly<{ children: ReactNode }>) {
  const { isAuthenticated, access } = useAuth();
  if (isAuthenticated && access && !access.allowed) {
    return <IneligiblePage />;
  }
  return <>{children}</>;
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
          <AccessGate>
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
                <Route index element={<Navigate to="/admin/semesters" replace />} />
                <Route path="semesters" element={<SemestersPage />} />
                <Route path="semesters/:id/roster" element={<SemesterRosterPage />} />
                <Route path="users" element={<UsersPage />} />
                <Route path="activity-logs" element={<ActivityLogsPage />} />
                <Route path="settings" element={<SettingsPage />} />
                <Route path="support" element={<SupportPage />} />
                <Route path="profile" element={<ProfilePage />} />
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
                <Route path="registrations" element={<LecturerRepositoryPage />} />
                <Route path="groups" element={<LecturerGroupsPage />} />
                <Route path="groups/:id" element={<LecturerGroupDetailPage />} />
                <Route path="create" element={<TopicCreatePage />} />
                <Route path="moderate" element={<LecturerModerationPage />} />
                <Route path="moderate/:id" element={<LecturerReviewPage />} />
                <Route path="history" element={<LecturerHistoryPage />} />
                <Route path="supervised-projects" element={<SupervisedProjectsPage />} />
                <Route path="support" element={<LecturerSupportPage />} />
                <Route path="profile" element={<ProfilePage />} />
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
                <Route
                  path="assign/:tab"
                  element={
                    <ProtectedRoute allowedRoles={["departmenthead"]}>
                      <AssignEvaluatorsPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="checklist-config"
                  element={
                    <ProtectedRoute allowedRoles={["departmenthead"]}>
                      <ChecklistConfigPage />
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
                <Route path="groups/:tab" element={<StudentGroupPage />} />
                <Route path="support" element={<StudentSupportPage />} />
                <Route path="profile" element={<ProfilePage />} />
              </Route>

              {/* Smart redirect: root goes to role-based home */}
              <Route path="/" element={<RoleBasedRedirect />} />


              {/* 404 — any unmatched route */}
              <Route path="*" element={<NotFoundPage />} />
            </Routes>
          </AnimatePresence>
          </AccessGate>
          </BrandingProvider>
        </SystemErrorProvider>
      </AuthProvider>
    </MaintenanceProvider>
  );
}

export default App;
