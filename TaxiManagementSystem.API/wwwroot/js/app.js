const API_BASE_URL = "http://172.16.7.10:8080";  // Need to replace with  partner's URL 


// for dashboard cards, fecth counts from API and update the innerText of the respective elements
//ダッシュボードカードの場合、APIからカウントを取得し、それぞれの要素のinnerTextを更新します。
async function loadDashboardCards() {

    try {

        //   API_BASE_URL}/api/jobs/count
        const active = await fetch(`${API_BASE_URL}/api/jobs/count`);
        //API_BASE_URL}/api/jobs/history/count/today`
        const completed = await fetch(`${API_BASE_URL}/api/jobs/history/count/today`);
        //API_BASE_URL}/api/taxis/count
        const taxis = await fetch(`${API_BASE_URL}/api/taxis/count`);
        //API_BASE_URL}/api/taxis/available/count
        const available = await fetch(`${API_BASE_URL}/api/taxis/available/count`);
        console.log(active);
        document.getElementById("activeJobsCount").innerText = (await active.json()).count;
        document.getElementById("completedJobsCount").innerText = (await completed.json()).count;
        document.getElementById("totalTaxiCount").innerText = (await taxis.json()).count;
        document.getElementById("availableTaxiCount").innerText = (await available.json()).count;
        //*************Added for test  *********************************************/
        // const response =
        //     await fetch(`${API_BASE_URL}/counts`);

        // const counts =
        //     await response.json();

        // document.getElementById("activeJobsCount").innerText =
        //     counts.activeJobs;

        // document.getElementById("completedJobsCount").innerText =
        //     counts.completedToday;

        // document.getElementById("totalTaxiCount").innerText =
        //     counts.totalTaxis;

        // document.getElementById("availableTaxiCount").innerText =
        //     counts.availableTaxis;

    }
    catch (err) {
        console.error(err);
    }
}

// 1. Flag to keep track of whether the dashboard has loaded its initial data
// ダッシュボードが初期データを読み込んだかどうかを追跡するためのフラグ
let isInitialLoad = true;
// 2. This runs automatically when the browser window finishes loading
//これはブラウザウィンドウの読み込みが完了すると自動的に実行されます。
window.addEventListener('DOMContentLoaded', async () => {
    await loadJobs();
    loadDashboardCards();
    isInitialLoad = false; // 最初の読み込みが終わったらすぐにフラグをオフにする_Turn off the flag immediately after the first load
});

//loadJobs();
//Reload button processing
//リロードボタンの処理
async function handleReload(buttonElement) {
    buttonElement.classList.add("is-loading");

    await loadJobs(true);
    await loadDashboardCards();

    await new Promise(resolve =>
        setTimeout(resolve, 1000)
    );

    buttonElement.classList.remove("is-loading");
    // try
    // {
    //     buttonElement.classList.add('is-loading');

    //     await loadJobs(true);
    //     await loadDashboardCards();

    //     console.log("Reload completed");
    // }
    // catch(error)
    // {
    //     console.error(error);
    // }
    // finally
    // {
    //     buttonElement.classList.remove('is-loading');
    // }
}
// Keep the spinner for at least 1 second for better UX   

// 3. This function fetches the list of available taxis from the API
//この関数は、APIから利用可能なタクシーのリストを取得します。
async function getAvailableTaxis() {

    const response =
        await fetch(
            //`${API_BASE_URL}/api/taxis/available`
            `${API_BASE_URL}/api/taxis/available`
        );

    return await response.json();

}

// const taxis =  getAvailableTaxis();

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

//function for display main dashboard page data and reload processing
//メインダッシュボードページのデータ表示と再読み込み処理を行う関数()
async function loadJobs(isManualReload = false) {
    console.log("loadJobs started");
    if (!isInitialLoad && !isManualReload) {
        console.log("Skipping update. Dashboard only updates when reload is clicked.");
        console.log("loadJobs completed");
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/api/jobs`);
        const jobs = await response.json();
        console.log("Jobs fetched:", jobs);

        const availableTaxis = await getAvailableTaxis();

        const tbody = document.querySelector("#jobTable tbody");
        console.log("tbody =", tbody);
        console.log("jobs count =", jobs.length);
        console.log("first job =", jobs[0]);
        tbody.innerHTML = "";

        jobs.forEach(job => {
            let taxiCell = "";
            //.filter(t => t.status === "Idle")
            if (!job.taxiId) {
                const idleOptions = availableTaxis

                    .map(t =>
                        `<option value="${t.taxiId}">
                            ${t.taxiId}
                        </option>`
                    )
                    .join("");

                taxiCell = `
                    <select class="modern-select"
                        onchange="assignTaxi('${job.jobId}',this.value)">
                        <option value="" disabled selected>
                            Select
                        </option>
                        ${idleOptions}
                    </select>
                `;
            }
            else {
                taxiCell = job.taxiId;
            }

            const actionText =
                (job.status === "Queued" ||
                    job.status === "Waiting")
                    ? "Cancel"
                    : "Abort";

            const actionClass =
                actionText === "Cancel"
                    ? "cancel"
                    : "abort";

            tbody.innerHTML += `
                <tr>
                    <td>${job.jobId}</td>
                    <td>
                       <span class="status-badge ${job.status}">
                          ${job.status}
                       </span>
                    </td>
                    <td>${job.fromLoc}</td>
                    <td>${job.toLoc}</td>
                    <td>${taxiCell}</td>
                    <td>${job.driverName ?? "N/A"}</td>
                    <td>
                        <button
                            class="action-btn ${actionClass}"
                            onclick="updateJobStatus('${job.jobId}','${job.status}')">
                            ${actionText}
                        </button>
                    </td>
                </tr>
            `;
        });
    }
    catch (error) {
        console.error(error);
    }
}

// 2. This function is called when the user selects a taxi from the dropdown 
//この関数は、ユーザーがドロップダウンリストからタクシーを選択したときに呼び出されます。

async function assignTaxi(jobId, taxiId) {

    const response =
        await fetch(
            `${API_BASE_URL}/api/jobs/${jobId}/reassign`,
            {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    taxiId: taxiId
                })
            }
        );

    if (response.ok) {

        alert("Taxi Assigned");

        loadJobs(true);

    }
    else {

        const error =
            await response.json();

        alert(error.error);

    }
}

//Cancel ボタン
async function cancelJob(jobId) {

    const response =
        await fetch(
            `${API_BASE_URL}/api/jobs/${jobId}/cancel`,
            {
                method: "PUT"
            }
        );

    // if(response.status === 204){

    //     loadJobs(true);

    // }
    if (response.ok) {
        alert("Job Cancelled Successfully");
        await loadJobs(true);
    }
    else {
        alert("Cancel Failed");
    }

}

//Abort ボタン
async function abortJob(jobId) {

    const response =
        await fetch(
            `${API_BASE_URL}/api/jobs/${jobId}/abort`,
            {
                method: "PUT"
            }
        );

    // if(response.status === 204){

    //     loadJobs(true);

    // }
    if (response.ok) {
        alert("Job Aborted Successfully");
        await loadJobs(true);
    }
    else {
        alert("Abort Failed");
    }

}

function updateJobStatus(jobId, status) {
    // if (status === "Queued" ||
    //     status === "Waiting") {
    //     cancelJob(jobId);
    // }
    // else {
    //     abortJob(jobId);
    // }

    //************Changed here ******************** */
    let action = "";

    if (status === "Queued" || status === "Waiting") {
        action = "Cancel";
    }
    else {
        action = "Abort";
    }

    const confirmed = confirm(
        `Are you sure you want to ${action} Job ${jobId}?`
    );

    if (!confirmed) {
        return;
    }

    if (action === "Cancel") {
        cancelJob(jobId);
    }
    else {
        abortJob(jobId);
    }
}


async function loadTaxiDropdown() {
    const dropdown =
        document.getElementById("taxiDropdown");

    const taxis =
        await getAvailableTaxis();

    console.log(taxis);

    dropdown.innerHTML =
        '<option value="">Select Taxi</option>';

    taxis.forEach(taxi => {
        dropdown.innerHTML +=
            `<option value="${taxi.taxiId}">
                ${taxi.taxiId}
             </option>`;
    });

    dropdown.disabled =
        taxis.length === 0;
}

async function submitJob() {

    const fromLoc = document.getElementById("fromInput").value;
    const toLoc = document.getElementById("toInput").value;
    //const taxiId = document.getElementById("taxiDropdown").value || null;
    const taxiDropdown =
    document.getElementById("taxiDropdown");

    const taxiId =
    taxiDropdown.value === "" ? null : taxiDropdown.value;
    if (!fromLoc || !toLoc) {
        alert("Please enter FROM and TO locations.");
        return;
    }

    const confirmed = confirm(
        `Create Job?\n\nFROM: ${fromLoc}\nTO: ${toLoc}\nTaxi: ${taxiId || null}`
    );

    if (!confirmed) {
        return;
    }
    const response =
        await fetch(
            `${API_BASE_URL}/api/jobs`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    jobId: "J" + Date.now(),
                    status: "Queued",
                    fromLoc,
                    toLoc,
                    taxiId,
                    driverName: null
                })
            }
        );

        console.log(response);

    if (response.status === 201) {
        alert("Job Created");
        await loadJobs(true);

        showPage("dashboard");

    }
    else {
        const error = await response.json();
        alert(error.error);

    }
}


//for Taxi State page 
async function loadTaxiState() {

    const response =
        await fetch(
            `${API_BASE_URL}/api/taxis`
        );

    const taxis =
        await response.json();

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
            <td>${taxi.driverName}</td>
            <td>${taxi.jobId ?? "N/A"}</td>
        </tr>
        `;

    });
}

//For History Page
async function loadHistory() {

    const response = await fetch(
        `${API_BASE_URL}/api/jobs/history`
    );

    const jobs =
        await response.json();

    const tbody =
        document.querySelector(
            "#historyTable tbody"
        );

    tbody.innerHTML = "";

    jobs.forEach(job => {

        tbody.innerHTML += `
        <tr>
            <td>${job.jobId}</td>
            <td>${job.status}</td>
            <td>${job.fromLoc}</td>
            <td>${job.toLoc}</td>
            <td>${job.taxiId ?? "-"}</td>
            <td>${job.driverName ?? "-"}</td>
            <td>${job.closedAt}</td>
        </tr>
        `;

    });
}



