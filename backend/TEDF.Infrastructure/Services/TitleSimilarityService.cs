using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Evaluations.DTOs;
using TEDF.Persistence.SqlServer;

namespace TEDF.Infrastructure.Services;

public class TitleSimilarityService : ITitleSimilarityService
{
    private readonly AppDbContext _context;

    // ── Stop words ───────────────────────────────────────────────────────────
    // General English stop words only — do NOT include domain terms like
    // "system", "management", "platform" because they are meaningful for topic comparison.
    private static readonly HashSet<string> EnglishStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "of", "for", "and", "in", "on", "to", "with", "by", "at", "from",
        "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does",
        "did", "will", "would", "could", "should", "may", "might", "shall", "can",
        "this", "that", "these", "those", "it", "its", "not", "no", "or", "but", "if", "then",
        "than", "so", "as", "up", "out", "about", "into", "through", "during", "before", "after",
        "above", "below", "between", "under", "over", "each", "every", "all", "both", "few",
        "more", "most", "other", "some", "such", "only", "own", "same", "also", "very",
        "using", "based", "via",
        "university"
    };

    // Technology terms — filter these out to focus similarity on TOPIC, not tech stack.
    private static readonly HashSet<string> TechStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Web frameworks & libraries
        "react", "reactjs", "angular", "vue", "vuejs", "next", "nextjs", "nuxt", "svelte",
        "jquery", "bootstrap", "tailwind", "material", "blazor", "razor",
        // Backend frameworks
        "asp", "aspnet", "net", "dotnet", "spring", "springboot", "django", "flask",
        "laravel", "express", "fastapi", "rails", "nestjs", "koa",
        // Runtime & platforms
        "nodejs", "node", "deno", "bun",
        // Languages
        "javascript", "typescript", "python", "java", "csharp", "php", "ruby", "golang",
        "swift", "kotlin", "dart", "rust", "scala", "elixir",
        // Databases (specific engines + generic term)
        "database", "databases",
        "sql", "mysql", "postgresql", "postgres", "mongodb", "redis", "sqlite", "oracle",
        "sqlserver", "firebase", "supabase", "dynamodb", "cassandra", "neo4j", "elasticsearch",
        // Mobile
        "flutter", "ionic", "xamarin", "maui", "swiftui", "jetpack",
        "native",  // "React Native" — "react" already filtered
        // Cloud & DevOps
        "aws", "azure", "gcp", "docker", "kubernetes", "heroku", "vercel", "netlify",
        "cicd", "devops", "pipeline",
        // API & protocols
        "api", "rest", "restful", "graphql", "grpc", "websocket", "signalr", "oauth", "jwt",
        // Generic tech fragments
        "js", "css", "html", "http", "json", "xml", "mvc", "mvvm", "mvp",
        "wpf", "winforms", "entity", "framework",
        "microservice", "microservices", "serverless",
        "core", "server", "client", "frontend", "backend", "fullstack",
        "app", "web", "mobile", "desktop", "cloud",
        // UI/UX
        "interface", "ui", "ux", "dashboard", "widget",
        // AI/ML methodologies — technique, not topic
        "machine", "iot",
        "neural", "network", "deep", "learning",  // "deep learning", "neural network"
        "nlp", "natural", "language", "processing",
        "algorithm", "algorithms",
        "model", "models",
        // Blockchain / crypto
        "blockchain", "crypto", "nft", "token", "smart", "contract",
        // Embedded / hardware
        "embedded", "sensor", "sensors", "arduino", "raspberry", "firmware",
        // Data infrastructure — generic, not domain-specific
        "data", "dataset", "pipeline", "warehouse", "hadoop", "spark",
        // Time & series
        "time", "series",
        // Generic action verbs — appear in almost every thesis, not discriminating
        "develop", "build", "create", "construct", "implement", "design",
        // Generic adjectives
        "intelligent", "smart", "advanced", "comprehensive", "real", "automated",
        // Monitor/track/analyze — what you monitor IS the topic
        "monitor", "track", "surveil",
        "analyz", "analysis", "assess",
        // Generic nouns — "build a system/platform" says nothing about the topic
        "system", "platform", "applic", "software", "tool", "solut", "program",
        // "boot" is part of "Spring Boot"
        "boot",
        // Management/admin — too generic ("manage a hotel" ≠ "manage expenses")
        "manage", "management", "administr", "organiz", "governance"
    };

    // Proper nouns — Vietnamese city/location name tokens that appear in English titles
    // but carry no topic-discriminating value.
    // Titles are split on spaces, so "Da Nang" → ["da", "nang"], "Ho Chi Minh" → ["ho", "chi", "minh"].
    // Tokens ≤ 2 chars ("da", "ho", "ha") are already filtered by the min-length check (> 2).
    // This list covers tokens with 3+ chars that survive the length filter.
    private static readonly HashSet<string> ProperNounStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Đà Nẵng → "nang" (4 chars, "da" filtered by length)
        "nang",
        // Hà Nội → "noi" (3 chars, "ha" filtered by length)
        "noi",
        // Hồ Chí Minh → "chi" (3), "minh" (4) — "ho" filtered by length
        "chi", "minh", "sai", "gon",
        // Huế → "hue" (3 chars)
        "hue",
        // Nha Trang → "nha" (3), "trang" (5)
        "nha", "trang",
        // Cần Thơ → "can" already in EnglishStopWords, "tho" (3)
        "tho",
        // Hải Phòng → "hai" (3), "phong" (5)
        "hai", "phong",
        // Biên Hòa → "bien" (4), "hoa" (3)
        "bien", "hoa",
        // Vũng Tàu → "vung" (4), "tau" (3)
        "vung", "tau",
        // Quy Nhơn → "quy" (3), "nhon" (4)
        "quy", "nhon",
        // Đà Lạt → "lat" (3 chars, "da" filtered by length)
        "lat", "dalat",
        // Buôn Ma Thuột → "buon" (4), "thuot" (5)
        "buon", "thuot",
        // Pleiku → "pleiku" (6)
        "pleiku",
        // Vinh → "vinh" (4)
        "vinh",
        // Institutions
        "fpt",
        // Country
        "viet", "nam",
    };

    // ── Synonym mapping ──────────────────────────────────────────────────────
    // Maps stemmed tokens to canonical forms so semantically equivalent words
    // are treated as identical. E.g. "develop" and "build" both → "#BUILD".
    private static readonly Dictionary<string, string> SynonymMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Online/Digital → #DIGITAL
        {"online", "#DIGITAL"}, {"virtual", "#DIGITAL"}, {"electron", "#DIGITAL"},
        {"digital", "#DIGITAL"},
        // Detect/Recognize → #DETECT
        {"detect", "#DETECT"}, {"recogn", "#DETECT"}, {"identif", "#DETECT"},
        // Predict/Forecast → #PREDICT
        {"predict", "#PREDICT"}, {"forecast", "#PREDICT"}, {"estimat", "#PREDICT"},
        // Automate → #AUTOMATE
        {"automat", "#AUTOMATE"},
        // Education/Learning → #EDUCATION
        {"learn", "#EDUCATION"}, {"educat", "#EDUCATION"}, {"train", "#EDUCATION"},
        {"teach", "#EDUCATION"}, {"tutor", "#EDUCATION"}, {"school", "#EDUCATION"},
        // Health/Medical → #HEALTH
        {"health", "#HEALTH"}, {"medic", "#HEALTH"}, {"clinic", "#HEALTH"},
        {"hospital", "#HEALTH"}, {"patient", "#HEALTH"}, {"diagnos", "#HEALTH"},
        // Commerce/Shopping → #COMMERCE
        {"commerce", "#COMMERCE"}, {"ecommerce", "#COMMERCE"}, {"shop", "#COMMERCE"},
        {"store", "#COMMERCE"}, {"market", "#COMMERCE"}, {"retail", "#COMMERCE"},
        {"bookstore", "#COMMERCE"}, {"sell", "#COMMERCE"}, {"purchas", "#COMMERCE"},
        // Support/Assist → #SUPPORT
        {"support", "#SUPPORT"}, {"assist", "#SUPPORT"}, {"help", "#SUPPORT"},
        // Recommend/Suggest → #RECOMMEND
        {"recommend", "#RECOMMEND"}, {"suggest", "#RECOMMEND"},
        // Schedule/Reserve → #SCHEDULE
        {"schedul", "#SCHEDULE"}, {"reserv", "#SCHEDULE"},
        // Chat/Message → #COMMUNICATE
        {"chat", "#COMMUNICATE"}, {"messag", "#COMMUNICATE"}, {"communicat", "#COMMUNICATE"},
        // Inventory/Stock → #INVENTORY
        {"inventory", "#INVENTORY"}, {"stock", "#INVENTORY"}, {"warehous", "#INVENTORY"},
        // Notification/Alert → #NOTIFY
        {"notif", "#NOTIFY"}, {"alert", "#NOTIFY"}, {"remind", "#NOTIFY"},
    };

    // Human-readable display names for canonical synonym forms
    private static readonly Dictionary<string, string> SynonymDisplayNames = new()
    {
        {"#DIGITAL", "online/digital"},
        {"#DETECT", "detect/recognize"},
        {"#PREDICT", "predict/forecast"},

        {"#AUTOMATE", "automate"},
        {"#EDUCATION", "education/learning"},
        {"#HEALTH", "health/medical"},
        {"#COMMERCE", "commerce/shop"},

        {"#SUPPORT", "support/assist"},
        {"#RECOMMEND", "recommend"},
        {"#SCHEDULE", "schedule/reserve"},
        {"#COMMUNICATE", "chat/message"},
        {"#INVENTORY", "inventory/stock"},
        {"#NOTIFY", "notify/alert"},
    };

    public TitleSimilarityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SimilarTitleDto>> FindSimilarTitlesAsync(
        Guid projectId,
        int topN = 3,
        CancellationToken cancellationToken = default)
    {
        // 1. Get the current project's English title
        var currentProject = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new { p.Id, NameEn = p.NameEn.ToString(), p.SemesterId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy đề tài.");

        // 2. Get the 3 most recent semesters (2 recent + upcoming/current)
        var recentSemesterIds = await _context.Semesters
            .AsNoTracking()
            .OrderByDescending(s => s.StartDate)
            .Take(3)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        // 3. Get all projects from these semesters (excluding current project)
        //    Include additional fields for comparison panel.
        var corpus = await (
            from p in _context.Projects.AsNoTracking()
            where recentSemesterIds.Contains(p.SemesterId) && p.Id != projectId
            join s in _context.Semesters.AsNoTracking() on p.SemesterId equals s.Id
            select new
            {
                p.Id,
                Code = p.Code.ToString(),
                NameEn = p.NameEn.ToString(),
                NameVi = p.NameVi.ToString(),
                SemesterName = s.Name,
                Description = p.Description,
                Objectives = p.Objectives,
                p.Scope,
                Technologies = p.Technologies != null ? p.Technologies.ToString() : null,
                p.ExpectedResults,
                p.GroupId
            }
        ).ToListAsync(cancellationToken);

        if (corpus.Count == 0)
            return [];

        // 3b. Batch-load mentor & student names for all corpus projects
        var corpusProjectIds = corpus.Select(c => c.Id).ToList();
        var corpusGroupIds = corpus.Where(c => c.GroupId != null).Select(c => c.GroupId!.Value).Distinct().ToList();

        var mentorNames = await (
            from pm in _context.ProjectMentors.AsNoTracking()
            where corpusProjectIds.Contains(pm.ProjectId) && pm.Status == Domain.Enums.Mentor.ProjectMentorStatus.Active
            join u in _context.Users.AsNoTracking() on pm.MentorId equals u.Id
            select new { pm.ProjectId, u.FullName }
        ).GroupBy(x => x.ProjectId)
         .ToDictionaryAsync(g => g.Key, g => g.First().FullName, cancellationToken);

        var studentNames = corpusGroupIds.Count > 0
            ? await (
                from gm in _context.GroupMembers.AsNoTracking()
                where corpusGroupIds.Contains(gm.GroupId) && gm.Role == Domain.Enums.Group.GroupMemberRole.Leader
                join u in _context.Users.AsNoTracking() on gm.StudentId equals u.Id
                select new { gm.GroupId, u.FullName }
            ).GroupBy(x => x.GroupId)
             .ToDictionaryAsync(g => g.Key, g => g.First().FullName, cancellationToken)
            : new Dictionary<Guid, string>();

        // 4. Preprocess all titles (with synonym mapping)
        var currentTokens = Preprocess(currentProject.NameEn);
        var corpusData = corpus.Select(c => new
        {
            c.Id,
            c.Code,
            c.NameEn,
            c.NameVi,
            c.SemesterName,
            c.Description,
            c.Objectives,
            c.Scope,
            c.Technologies,
            c.ExpectedResults,
            c.GroupId,
            Tokens = Preprocess(c.NameEn)
        }).ToList();

        // 5. Build vocabulary and compute IDF
        var allDocuments = new List<List<string>> { currentTokens };
        allDocuments.AddRange(corpusData.Select(c => c.Tokens));

        int totalDocs = allDocuments.Count;
        var vocabulary = allDocuments
            .SelectMany(d => d.Distinct())
            .Distinct()
            .ToList();

        var idf = new Dictionary<string, double>();
        foreach (var term in vocabulary)
        {
            int df = allDocuments.Count(d => d.Contains(term));
            // Smoothed IDF to avoid zero-weight for very common terms
            idf[term] = Math.Log(1.0 + (double)totalDocs / df);
        }

        // 6. Compute TF-IDF vector for current project
        var currentVector = ComputeTfIdfVector(currentTokens, idf, vocabulary);

        // 7. Compute TF-IDF Cosine similarity
        var results = new List<(int index, double similarity, List<string> commonKeywords)>();
        var currentTokenSet = new HashSet<string>(currentTokens);

        for (int i = 0; i < corpusData.Count; i++)
        {
            var docTokens = corpusData[i].Tokens;
            if (docTokens.Count == 0) continue;

            var docVector = ComputeTfIdfVector(docTokens, idf, vocabulary);
            double similarity = CosineSimilarity(currentVector, docVector);

            if (similarity > 0.01)
            {
                // Map canonical forms to display names for common keywords
                var docTokenSet = new HashSet<string>(docTokens);
                var commonKeywords = currentTokenSet
                    .Intersect(docTokenSet)
                    .Select(k => SynonymDisplayNames.GetValueOrDefault(k, k))
                    .ToList();
                results.Add((i, similarity, commonKeywords));
            }
        }

        // 8. Deduplicate by normalized English title (keep highest score per unique title)
        var deduplicated = results
            .OrderByDescending(r => r.similarity)
            .DistinctBy(r => corpusData[r.index].NameEn.Trim().ToLowerInvariant())
            .Take(topN)
            .ToList();

        // 9. Map to DTOs with all fields for comparison panel
        return deduplicated
            .Select(r =>
            {
                var item = corpusData[r.index];
                return new SimilarTitleDto
                {
                    ProjectId = item.Id,
                    ProjectCode = item.Code,
                    NameEn = item.NameEn,
                    NameVi = item.NameVi,
                    SemesterName = item.SemesterName,
                    Similarity = Math.Round(r.similarity * 100, 1),
                    CommonKeywords = r.commonKeywords,
                    Description = item.Description,
                    Objectives = item.Objectives,
                    Scope = item.Scope,
                    Technologies = item.Technologies,
                    ExpectedResults = item.ExpectedResults,
                    MentorName = mentorNames.GetValueOrDefault(item.Id, ""),
                    StudentName = item.GroupId.HasValue
                        ? studentNames.GetValueOrDefault(item.GroupId.Value, "")
                        : ""
                };
            })
            .ToList();
    }

    /// <summary>
    /// Preprocesses a title: lowercase → remove punctuation → tokenize →
    /// remove stop words → conservative stem → synonym mapping → deduplicate.
    /// </summary>
    private static List<string> Preprocess(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return [];

        // Lowercase and normalize separators (dots, slashes, hyphens → spaces)
        var cleaned = new string(title.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) || c == ' ' ? c : ' ')
            .ToArray());

        return cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)                        // min 3 chars — filters "da", "ho", "js", etc.
            .Where(t => !EnglishStopWords.Contains(t))
            .Where(t => !TechStopWords.Contains(t))          // pre-stem filter
            .Where(t => !ProperNounStopWords.Contains(t))
            .Select(ConservativeStem)
            .Where(t => !TechStopWords.Contains(t))          // post-stem filter (catches "building"→"build")
            .Select(t => SynonymMap.GetValueOrDefault(t, t)) // Map synonyms to canonical forms
            .Distinct() // Each concept counts once per document
            .ToList();
    }

    /// <summary>
    /// Conservative stemmer — only strips clear, unambiguous suffixes on long words.
    /// Avoids mistakes like "career" → "care" or "online" → "onl".
    /// </summary>
    private static string ConservativeStem(string word)
    {
        if (word.Length <= 5)
            return word;

        // Only handle suffixes where removal is clearly correct
        // and the remaining stem is long enough (>= 4 chars)
        if (word.EndsWith("ation") && word.Length >= 9)
            return word[..^5]; // preparation → prepar, application → applic
        if (word.EndsWith("tion") && word.Length >= 8)
            return word[..^4];
        if (word.EndsWith("sion") && word.Length >= 8)
            return word[..^4];
        if (word.EndsWith("ment") && word.Length >= 8)
            return word[..^4]; // management → manage
        if (word.EndsWith("ness") && word.Length >= 8)
            return word[..^4];
        if (word.EndsWith("ling") && word.Length >= 7)
            return word[..^4]; // handling → hand
        if (word.EndsWith("ying") && word.Length >= 7)
            return word[..^4]; // applying → appl
        if (word.EndsWith("ing") && word.Length >= 7)
            return word[..^3]; // developing → develop, building → build
        if (word.EndsWith("ized") && word.Length >= 8)
            return word[..^4];
        if (word.EndsWith("ised") && word.Length >= 8)
            return word[..^4];

        // Plural 's' — very conservative: only on long words, skip -ss, -us, -is
        if (word.EndsWith('s') && !word.EndsWith("ss") && !word.EndsWith("us")
            && !word.EndsWith("is") && word.Length > 6)
            return word[..^1];

        return word;
    }

    /// <summary>
    /// Computes TF-IDF vector for a document.
    /// Uses log-normalized TF: 1 + log(rawTf) if rawTf > 0.
    /// </summary>
    private static double[] ComputeTfIdfVector(
        List<string> tokens,
        Dictionary<string, double> idf,
        List<string> vocabulary)
    {
        var tf = new Dictionary<string, int>();
        foreach (var token in tokens)
            tf[token] = tf.GetValueOrDefault(token) + 1;

        var vector = new double[vocabulary.Count];
        for (int i = 0; i < vocabulary.Count; i++)
        {
            var term = vocabulary[i];
            if (tf.TryGetValue(term, out var freq) && idf.TryGetValue(term, out var idfValue))
            {
                // Log-normalized TF to reduce impact of repeated terms
                double logTf = 1.0 + Math.Log(freq);
                vector[i] = logTf * idfValue;
            }
        }

        return vector;
    }

    /// <summary>
    /// Cosine similarity between two vectors: cos(A,B) = (A·B) / (‖A‖ × ‖B‖)
    /// </summary>
    private static double CosineSimilarity(double[] a, double[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        var denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denominator == 0 ? 0 : dot / denominator;
    }

}
