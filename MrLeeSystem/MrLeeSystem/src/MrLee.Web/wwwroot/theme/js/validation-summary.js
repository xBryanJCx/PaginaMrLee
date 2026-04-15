document.addEventListener("DOMContentLoaded", () => {
    const summarySelector = "[data-valmsg-summary='true']";

    const syncSummary = (summary) => {
        const hasVisibleMessage = Array.from(summary.querySelectorAll("li")).some((item) => {
            const text = (item.textContent || "").trim();
            const isHidden = item.hidden || item.style.display === "none";
            return text.length > 0 && !isHidden;
        });

        summary.style.display = hasVisibleMessage ? "" : "none";
    };

    const syncSummaries = (scope = document) => {
        scope.querySelectorAll(summarySelector).forEach(syncSummary);
    };

    syncSummaries();

    document.querySelectorAll("form").forEach((form) => {
        const queueSync = () => window.setTimeout(() => syncSummaries(form), 0);

        form.addEventListener("submit", queueSync);
        form.addEventListener("reset", queueSync);
        form.addEventListener("input", queueSync, true);
        form.addEventListener("change", queueSync, true);
    });
});
