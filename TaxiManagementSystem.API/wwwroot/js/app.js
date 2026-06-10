    const API_BASE_URL = "https://localhost:5001";
// Need to replace with  partner's URL later
    
    // let jobs = [
    //     {
    //     jobId: "J20260609-0051",
    //     jobStatus: "Active",
    //     from: "新居浜駅",
    //     to: "イオンモール新居浜",
    //     taxiId: "TX002",
    //     driver: "鈴木　次郎"
    //     },
    //     {
    //     jobId: "J20260609-0052",
    //     jobStatus: "Waiting",
    //     from: "フレッシュバリュー喜光地",
    //     to: "喜光地自治会館",
    //     taxiId: "TX003",
    //     driver: "高橋　三郎"
    //     },
    //     {
    //     jobId: "J20260609-0053",
    //     jobStatus: "Queued",
    //     from: "新須賀自治会館",
    //     to: "フジ新居浜",
    //     taxiId: null,
    //     driver: null
    //     },
    //     {
    //     jobId: "J20260609-0054",
    //     jobStatus: "Aborting", 
    //     from: "松山市",
    //     to: "今治市",
    //     taxiId: "TX004",
    //     driver: ""
    //     },
    //     {
    //     jobId: "J20260609-0055",
    //     jobStatus: "Active",
    //     from: "今治市",
    //     to: "松山市",
    //     taxiId: "TX005",
    //     driver: "越智健太"
    //     },
    //     {
    //     jobId: "J20260609-0056",
    //     jobStatus: "Waiting",  
    //     from: "西条市",
    //     to: "大洲市",
    //     taxiId: "",
    //     driver: "山田　太郎"
    //     },
    // ];

    // let taxis =
    //     [
    //         {
    //             taxiId: "TX001",
    //             status: "Idle",
    //             driver: "John"
    //         },
    //         {
    //             taxiId: "TX002",
    //             status: "Occupied",
    //             driver: "Mike"
    //         },
    //         {
    //             taxiId: "TX003",
    //             status: "Idle",
    //             driver: "Sara"
    //         },
    //         {
    //             taxiId: "TX004",
    //             status: "Reserved",
    //             driver: "Alice"
    //         },
    //         {
    //             taxiId: "TX005",
    //             status: "Occupied",
    //             driver: "Bob"
    //         },
    //         {
    //             taxiId: "TX006",
    //             status: "Offduty",
    //             driver: "Charlie"
    //         },
    //         {
    //             taxiId: "TX007",
    //             status: "Reserved",
    //             driver: "David"
    //         },
    //         {
    //             taxiId: "TX008",
    //             status: "Offduty",
    //             driver: "Eve"   
    //         },
    //     ];

async function loadDashboardCards() {

    try {

        const active =
            await fetch(`${API_BASE_URL}/api/jobs/count`);

        const completed =
            await fetch(`${API_BASE_URL}/api/jobs/history/count/today`);

        const taxis =
            await fetch(`${API_BASE_URL}/api/taxis/count`);

        const available =
            await fetch(`${API_BASE_URL}/api/taxis/available/count`);

        document.getElementById("activeJobsCount").innerText =
            (await active.json()).count;

        document.getElementById("completedJobsCount").innerText =
            (await completed.json()).count;

        document.getElementById("totalTaxiCount").innerText =
            (await taxis.json()).count;

        document.getElementById("availableTaxiCount").innerText =
            (await available.json()).count;

    }
    catch (err) {

        console.error(err);

    }
}

    // 1. Flag to keep track of whether the dashboard has loaded its initial data
    let isInitialLoad = true;
    // 2. This runs automatically when the browser window finishes loading
    window.addEventListener('DOMContentLoaded', () => {
        loadJobs();
        isInitialLoad = false; // Turn off the flag immediately after the first load
    });

    //loadJobs();
    function handleReload(buttonElement) {
        // 1. Start the visual spin animation and disable button
        buttonElement.classList.add('is-loading');

        // 2. Call your original data fetching function
        loadJobs(true);

        // 3. Simulating network delay (e.g., 1 second) then stop spinning
        // Replace this setTimeout with your actual API promise resolution if needed
        setTimeout(() => {
            buttonElement.classList.remove('is-loading');
        }, 1000);
    }






    function showPage(pageId) {
        document
            .querySelectorAll(".page")
            .forEach(page =>
                page.classList.remove("active"));

        document
            .getElementById(pageId)
            .classList.add("active");

        if (pageId === "taxiState")
            loadTaxiState();

        if (pageId === "history")
            loadHistory();

        if (pageId === "createJob")
            loadTaxiDropdown();
    }

    function loadJobs(isManualReload = false) {
        // IF it's not the initial browser launch AND the user DID NOT press reload, skip the update
        if (!isInitialLoad && !isManualReload) {
            console.log("Skipping update. Dashboard only updates when reload is clicked.");
            return;
        }

        console.log("Updating dashboard data...");
        const tbody =
            document.querySelector(
                "#jobTable tbody"
            );

tbody.innerHTML = "";

jobs.forEach(job => {
    let taxiCell;

    if (job.taxiId === "N/A" || !job.taxiId) {
        const idleOptions = taxis
            .filter(t => t.status === "Idle")
            .map(t => `<option value="${t.taxiId}">${t.taxiId}</option>`)
            .join("");

        // Added the 'modern-select' class for styling
        taxiCell = `
            <select class="modern-select" onchange="assignTaxi('${job.jobId}', this.value)">
                <option value="" disabled selected>Select</option>
                ${idleOptions}
            </select>
        `;
    } else {
        // Keeps flat string text structured uniformly inside cell
        taxiCell = `<span>${job.taxiId}</span>`;
    }

    const actionText = (job.jobStatus === "Queued" || job.jobStatus === "Waiting") ? "Cancel" : "ABORT";
    const actionClass = actionText === "Cancel" ? "cancel" : "abort";

    // Standard high-utility inline SVG illustration for Driver Icon
    const driverIconSvg = `
        <svg class="driver-icon" xmlns="http://w3.org" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M18 21a6 6 0 0 0-12 0"/>
            <circle cx="12" cy="10" r="4"/>
            <path d="M12 2v2"/>
        </svg>
    `;

    // Uniformly formats data rows using the fixed grid layouts
    tbody.innerHTML += `
        <tr>
            <td><strong>${job.jobId}</strong></td>
            <td><span class="status-badge ${job.jobStatus?.toLowerCase()}">${job.jobStatus || 'N/A'}</span></td>
            <td>${job.from || '—'}</td> 
            <td>${job.to || '—'}</td>
            <td>${taxiCell}</td>
            <td>
                <div class="driver-cell">
                    ${driverIconSvg}
                    <span>${job.driver || 'N/A'}</span>
                </div>
            </td>
            <td>
                <button class="action-btn ${actionClass}" onclick="updateJobStatus('${job.jobId}')">
                    ${actionText}
                </button>
            </td>
        </tr>
    `;
});
    }

    // 2. This function is called when the user selects a taxi from the dropdown 

    function assignTaxi(jobId, taxiId) {
        alert(
            `PUT Assign Taxi:
    ${jobId}
    -> ${taxiId}`
        );
    }

    function updateJobStatus(jobId) {
        alert(
            `PUT Status Update:
    ${jobId}`
        );
    }

    function loadTaxiDropdown() {
        const dropdown =
            document.getElementById(
                "taxiDropdown"
            );

        const idleTaxis =
            taxis.filter(
                t => t.status === "Idle"
            );

        if (idleTaxis.length > 0) {
            dropdown.disabled = false;

            dropdown.innerHTML =
                idleTaxis.map(
                    t =>
                        `<option>${t.taxiId}</option>`
                ).join("");
        }
        else {
            dropdown.disabled = true;
        }
    }

    function submitJob() {
        alert(
            "POST Create Job"
        );
    }

    function loadTaxiState() {
        const tbody =
            document.querySelector(
                "#taxiTable tbody"
            );

        tbody.innerHTML = "";

        taxis.forEach(taxi => {
            tbody.innerHTML += `
    <tr>

    <td>${taxi.taxiId}</td>
    <td>${taxi.status}</td>
    <td>${taxi.driver}</td>

    </tr>
    `;
        });
    }

    function loadHistory() {
        const tbody =
            document.querySelector(
                "#historyTable tbody"
            );

        tbody.innerHTML = "";

        jobs.forEach(job => {
            tbody.innerHTML += `
    <tr>

    <td>${job.jobId}</td>
    <td>${job.from}</td>
    <td>${job.jobStatus}</td>
    <td>${job.to}</td>
    <td>${job.taxiId}</td>

    </tr>
    `;
        });
    }



