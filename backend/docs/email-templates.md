# Email templates (Firestore `emailTemplates`)

The bodies of every transactional email live in the Firestore `emailTemplates` collection, **not in
this repository** — the `firebase/firestore-send-email` extension renders them. This file is the
contract between the two: the backend picks a template name and supplies placeholder values, the
Firestore document decides what the reader sees.

**Document id = template name.** They are declared as constants in
`TEDF.Infrastructure/Services/Email/Firestore/MailTemplateNames.cs`; renaming one here without
renaming the Firestore document silently breaks delivery — the extension writes `delivery.error` on
the `mail` document and the backend never learns about it.

Each document carries three string fields: `subject`, `html`, `text`. Placeholders use Handlebars
(`{{name}}`). Every placeholder is always a string — the backend substitutes readable Vietnamese
copy ("Không xác định") rather than leaving a gap, so a template never has to guard against nulls.

## Placeholder reference

| Template | Recipient | Placeholders |
|---|---|---|
| `published-student-list` | eligible students | `recipientName`, `semesterName`, `announcementDate`, `detailUrl` |
| `published-lecturer-list` | assigned lecturers | `recipientName`, `semesterName`, `announcementDate`, `detailUrl` |
| `topic-proposed` | department head | `departmentHeadName`, `lecturerName`, `topicName`, `departmentName`, `proposedAt`, `detailUrl` |
| `evaluation-assigned` | evaluator | `evaluatorName`, `topicName`, `assignedBy`, `deadline`, `detailUrl` |
| `evaluation-completed` | mentor + department head | `recipientName`, `evaluatorName`, `topicName`, `completedAt`, `evaluationConclusion`, `detailUrl` |
| `evaluation-consensus-approved` | mentor + students | `recipientName`, `topicName`, `conclusion`, `detailUrl` |
| `evaluation-consensus-rejected` | mentor + students | `recipientName`, `topicName`, `conclusion`, `detailUrl` |
| `topic-final-decision` | mentor | `recipientName`, `topicName`, `finalDecision`, `decisionReason`, `decidedBy`, `decidedAt`, `detailUrl` |
| `group-invitation` | invited student | `recipientName`, `inviterName`, `groupCode`, `invitedAt`, `detailUrl` |
| `group-join-requested` | group leader | `recipientName`, `studentName`, `groupCode`, `requestedAt`, `detailUrl` |
| `group-join-decision` | requesting student | `recipientName`, `groupCode`, `decision`, `decidedAt`, `detailUrl` |
| `support-ticket-created` | all admins | `recipientName`, `ticketCode`, `ticketTitle`, `category`, `priority`, `createdAt`, `detailUrl` |
| `support-ticket-replied` | the other party | `recipientName`, `senderName`, `ticketCode`, `ticketTitle`, `repliedAt`, `detailUrl` |
| `support-ticket-resolved` | reporter | `recipientName`, `ticketCode`, `ticketTitle`, `resolvedAt`, `detailUrl` |

The admin "send test email" action does **not** use a template: `FirestoreEmailSender` writes an
inline `message: { subject, html }` document, so the test works even before any template exists.

## Bodies for the group and support templates

Paste these into the matching Firestore document. `text` is the plain-text fallback for mail clients
that refuse HTML — the extension sends both parts.

### `group-invitation`

- **subject**: `[TEDF] Lời mời tham gia nhóm {{groupCode}}`
- **html**:
```html
<p>Xin chào {{recipientName}},</p>
<p><strong>{{inviterName}}</strong> đã mời bạn tham gia nhóm <strong>{{groupCode}}</strong> lúc {{invitedAt}}.</p>
<p>Vui lòng phản hồi trước khi lời mời hết hạn.</p>
<p><a href="{{detailUrl}}">Xem lời mời trên hệ thống TEDF</a></p>
<p><small>Đây là email tự động, vui lòng không trả lời.</small></p>
```
- **text**: `Xin chào {{recipientName}}, {{inviterName}} đã mời bạn tham gia nhóm {{groupCode}} lúc {{invitedAt}}. Xem chi tiết: {{detailUrl}}`

### `group-join-requested`

- **subject**: `[TEDF] Yêu cầu tham gia nhóm {{groupCode}}`
- **html**:
```html
<p>Xin chào {{recipientName}},</p>
<p><strong>{{studentName}}</strong> đã gửi yêu cầu tham gia nhóm <strong>{{groupCode}}</strong> lúc {{requestedAt}}.</p>
<p>Bạn là nhóm trưởng, vui lòng duyệt hoặc từ chối yêu cầu này.</p>
<p><a href="{{detailUrl}}">Xử lý yêu cầu trên hệ thống TEDF</a></p>
<p><small>Đây là email tự động, vui lòng không trả lời.</small></p>
```
- **text**: `Xin chào {{recipientName}}, {{studentName}} đã xin tham gia nhóm {{groupCode}} lúc {{requestedAt}}. Xử lý tại: {{detailUrl}}`

### `group-join-decision`

`decision` is already rendered as "Được chấp nhận" or "Bị từ chối", so the template stays neutral.

- **subject**: `[TEDF] Yêu cầu tham gia nhóm {{groupCode}}: {{decision}}`
- **html**:
```html
<p>Xin chào {{recipientName}},</p>
<p>Yêu cầu tham gia nhóm <strong>{{groupCode}}</strong> của bạn: <strong>{{decision}}</strong>.</p>
<p>Thời điểm xử lý: {{decidedAt}}</p>
<p><a href="{{detailUrl}}">Xem nhóm của bạn trên hệ thống TEDF</a></p>
<p><small>Đây là email tự động, vui lòng không trả lời.</small></p>
```
- **text**: `Xin chào {{recipientName}}, yêu cầu tham gia nhóm {{groupCode}} của bạn: {{decision}} ({{decidedAt}}). Chi tiết: {{detailUrl}}`

### `support-ticket-created`

- **subject**: `[TEDF] Ticket mới {{ticketCode}} — ưu tiên {{priority}}`
- **html**:
```html
<p>Xin chào {{recipientName}},</p>
<p>Một yêu cầu hỗ trợ mới vừa được tạo lúc {{createdAt}}:</p>
<ul>
  <li>Mã ticket: <strong>{{ticketCode}}</strong></li>
  <li>Tiêu đề: {{ticketTitle}}</li>
  <li>Phân loại: {{category}}</li>
  <li>Mức ưu tiên: {{priority}}</li>
</ul>
<p><a href="{{detailUrl}}">Xử lý ticket trên hệ thống TEDF</a></p>
<p><small>Đây là email tự động, vui lòng không trả lời.</small></p>
```
- **text**: `Ticket mới {{ticketCode}} ({{priority}}) — {{ticketTitle}}. Phân loại: {{category}}. Tạo lúc {{createdAt}}. Xử lý tại: {{detailUrl}}`

### `support-ticket-replied`

The reply body is deliberately absent — a ticket can carry personal details and email leaves the
system's access control behind. The reader must open the app to read it.

- **subject**: `[TEDF] Phản hồi mới trên ticket {{ticketCode}}`
- **html**:
```html
<p>Xin chào {{recipientName}},</p>
<p><strong>{{senderName}}</strong> vừa gửi một phản hồi trên ticket <strong>{{ticketCode}}</strong> ({{ticketTitle}}) lúc {{repliedAt}}.</p>
<p><a href="{{detailUrl}}">Đọc phản hồi trên hệ thống TEDF</a></p>
<p><small>Đây là email tự động, vui lòng không trả lời.</small></p>
```
- **text**: `{{senderName}} đã phản hồi ticket {{ticketCode}} ({{ticketTitle}}) lúc {{repliedAt}}. Đọc tại: {{detailUrl}}`

### `support-ticket-resolved`

- **subject**: `[TEDF] Ticket {{ticketCode}} đã được xử lý`
- **html**:
```html
<p>Xin chào {{recipientName}},</p>
<p>Ticket <strong>{{ticketCode}}</strong> ({{ticketTitle}}) đã được đánh dấu là đã xử lý lúc {{resolvedAt}}.</p>
<p>Nếu vấn đề chưa được giải quyết, bạn có thể mở lại ticket trên hệ thống.</p>
<p><a href="{{detailUrl}}">Xem ticket trên hệ thống TEDF</a></p>
<p><small>Đây là email tự động, vui lòng không trả lời.</small></p>
```
- **text**: `Ticket {{ticketCode}} ({{ticketTitle}}) đã được xử lý lúc {{resolvedAt}}. Chi tiết: {{detailUrl}}`

## Two things that break delivery silently

- **`FirestoreMail:FrontendBaseUrl` unset.** `detailUrl` degrades to a relative path (`/student`)
  and every button in every email points nowhere. It is wired to `FRONTEND_PUBLIC_ORIGIN` in
  `docker-compose.yml`.
- **A TTL policy on the `mail` collection.** Exactly-once delivery relies on the mail document
  *still existing* — `FirestoreMailQueue` treats an `AlreadyExists` write as "already sent". If a TTL
  deletes those documents, re-publishing a roster months later re-sends to every student. Keep the
  extension's `TTL_EXPIRE_TYPE` on `never`.
