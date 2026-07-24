import { useEffect, useState } from 'react';
import { projectService } from "@/lib";
import { ProjectAuditLogResponse, ProjectAuditLogDto } from "@/types";

interface ProjectAuditLogListProps {
    projectId: string;
}

export function ProjectAuditLogList({ projectId }: ProjectAuditLogListProps) {
    const [data, setData] = useState<ProjectAuditLogResponse | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (!projectId) return;
        setLoading(true);
        setError(null);
        projectService.getAuditLogs(projectId)
            .then(setData)
            .catch(e => setError(e.message))
            .finally(() => setLoading(false));
    }, [projectId]);

    if (loading) {
        return <div className="animate-pulse flex flex-col gap-2">
            <div className="h-10 bg-slate-100 rounded w-full" />
            <div className="h-10 bg-slate-100 rounded w-full" />
        </div>;
    }

    if (error) {
        return <div className="text-sm text-red-600 bg-red-50 p-3 rounded-lg border border-red-100">{error}</div>;
    }

    if (!data || data.logs.length === 0) {
        return <div className="text-sm text-slate-500 italic">Chưa có lịch sử thao tác.</div>;
    }

    return (
        <div className="flex flex-col gap-4">
            <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-slate-700">Lịch sử (Revision: {data.revisionCount})</span>
            </div>
            
            <div className="relative border-l-2 border-slate-200 ml-3 space-y-6 pb-2">
                {data.logs.map((log: ProjectAuditLogDto) => (
                    <div key={log.id} className="relative pl-6">
                        <div className="absolute -left-[9px] top-1 w-4 h-4 rounded-full bg-white border-2 border-primary flex items-center justify-center" />
                        
                        <div className="flex flex-col gap-1">
                            <div className="flex items-baseline gap-2">
                                <span className="text-sm font-bold text-slate-800">{formatAction(log.action)}</span>
                                <span className="text-xs text-slate-400">
                                    {new Date(log.timestamp).toLocaleString('vi-VN')}
                                </span>
                            </div>
                            
                            {log.performedByName && (
                                <p className="text-sm text-slate-600">
                                    Bởi: <span className="font-medium text-slate-700">{log.performedByName}</span>
                                </p>
                            )}
                            
                            {log.metadata && (
                                <div className="mt-2 bg-slate-50 rounded p-3 border border-slate-100 text-xs text-slate-600 overflow-x-auto">
                                    <pre className="font-mono">{JSON.stringify(log.metadata, null, 2)}</pre>
                                </div>
                            )}
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}

function formatAction(action: string) {
    const actionMap: Record<string, string> = {
        'Submitted': 'Nộp đề tài',
        'Resubmitted': 'Nộp lại đề tài',
        'MentorApproved': 'GVHD duyệt',
        'MentorNeedsModification': 'GVHD yêu cầu chỉnh sửa',
        'Approved': 'Hội đồng duyệt',
        'NeedsModification': 'Hội đồng yêu cầu chỉnh sửa',
        'Rejected': 'Hội đồng từ chối',
        'MentorAssigned': 'Phân công GVHD',
        'DocumentUploaded': 'Tải lên tài liệu',
        'DocumentDeleted': 'Xóa tài liệu',
    };
    return actionMap[action] || action;
}
