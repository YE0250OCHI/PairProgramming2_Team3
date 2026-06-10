    const API_BASE_URL = "http://localhost:3000";  // Need to replace with  partner's URL 

   
    // for dashboard cards, fecth counts from API and update the innerText of the respective elements
async function loadDashboardCards() {

    try {

        const active = await fetch(`${API_BASE_URL}/api/jobs/count`);
        const completed = await fetch(`${API_BASE_URL}/api/jobs/history/count/today`);
        const taxis = await fetch(`${API_BASE_URL}/api/taxis/count`);
        const available = await fetch(`${API_BASE_URL}/api/taxis/available/count`);
        document.getElementById("activeJobsCount").innerText = (await active.json()).count;
        document.getElementById("completedJobsCount").innerText = (await completed.json()).count;
        document.getElementById("totalTaxiCount").innerText = (await taxis.json()).count;
        document.getElementById("availableTaxiCount").innerText = (await available.json()).count;

    }
    catch (err)
    {
        console.error(err);
    }
}

    // 1. Flag to keep track of whether the dashboard has loaded its initial data
    let isInitialLoad = true;
    // 2. This runs automatically when the browser window finishes loading
    window.addEventListener('DOMContentLoaded', () => {
        loadJobs();
        //loadDashboardCards();
        isInitialLoad = false; // Turn off the flag immediately after the first load
    });

    //loadJobs();
   async  function handleReload(buttonElement) 
    {
        // 1. Start the visual spin animation and disable button
        buttonElement.classList.add('is-loading');

        // 2. Call your original data fetching function
        await loadJobs(true);
        await loadDashboardCards();
        
            buttonElement.classList.remove("is-loading");
        
    }
 // Keep the spinner for at least 1 second for better UX   
    
    // 3. This function fetches the list of available taxis from the API
    async function getAvailableTaxis() 
    {

    const response =
        await fetch(
            `${API_BASE_URL}/api/taxis/available`
        );

    return await response.json();

  }

  const taxis =  getAvailableTaxis();




    function showPage(pageId) 
    {
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

async function loadJobs(isManualReload = false) 
{
     if (!isInitialLoad && !isManualReload)
        {
         console.log("Skipping update. Dashboard only updates when reload is clicked.");
         return;
      }
    try 
    {
        //const response = await fetch(`${API_BASE_URL}/api/jobs`);
        const response = await fetch(`${API_BASE_URL}/jobs`);
        const jobs = await response.json();
    }
    catch(error)
    {
         console.error(error);
    }
    // IF it's not the initial browser launch AND the user DID NOT press reload, skip the update
   

    console.log("Updating dashboard data...");
    const tbody =
        document.querySelector(
            "#jobTable tbody"
        );

    tbody.innerHTML = "";

    jobs.forEach(job => {
        let taxiCell;

        if (job.taxiId === null || !job.taxiId) {
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
        }
        else {
            // Keeps flat string text structured uniformly inside cell
            taxiCell = `<span>${job.taxiId}</span>`;
        }

        const actionText = (job.status === "Queued" || job.status === "Waiting") ? "Cancel" : "ABORT";
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
            <td><span class="status-badge ${job.status?.toLowerCase()}">${job.status || 'N/A'}</span></td>
            <td>${job.fromLocLocLoc || '—'}</td> 
            <td>${job.toLoc || '—'}</td>
            <td>${taxiCell}</td>
            <td>
                <div class="driver-cell">
                    ${driverIconSvg}
                    <span>${job.driverName || 'N/A'}</span>
                </div>
            </td>
            <td>
                <button class="action-btn ${actionClass}" onclick="updateJobStatus('${job.jobId}','${job.status}')">
                    ${actionText}
                </button>
            </td>
        </tr>
    `;
    });
}

    // 2. This function is called when the user selects a taxi from the dropdown 

async function assignTaxi(jobId,taxiId)
{

    const response =
        await fetch(
            `${API_BASE_URL}/jobs/${jobId}/reassign`,
            {
                method:"PUT",
                headers:{
                    "Content-Type":"application/json"
                },
                body:JSON.stringify({
                    taxiId:taxiId
                })
            }
        );

    if(response.ok){

        alert("Taxi Assigned");

        loadJobs(true);

    }
    else{

        const error =
            await response.json();

        alert(error.error);

    }
}

//Cancel Button
async function cancelJob(jobId)
{

    const response =
        await fetch(
            `${API_BASE_URL}/jobs/${jobId}/cancel`,
            {
                method:"PUT"
            }
        );

    if(response.status === 204){

        loadJobs(true);

    }

}

//Abort Button
async function abortJob(jobId)
{

    const response =
        await fetch(
            `${API_BASE_URL}/jobs/${jobId}/abort`,
            {
                method:"PUT"
            }
        );

    if(response.status === 204){

        loadJobs(true);

    }

}

    function updateJobStatus(jobId, jobStatus) 
    {
        if (status === "Queued" ||
            status === "Waiting") {
            cancelJob(jobId);
        }
        else {
            abortJob(jobId);
        }
    }

async function loadTaxiDropdown()
{
    const dropdown =
        document.getElementById("taxiDropdown");

    const taxis =
        await getAvailableTaxis();

    dropdown.innerHTML =
        '<option value="">Select Taxi</option>';

    taxis.forEach(taxi =>
    {
        dropdown.innerHTML +=
            `<option value="${taxi.taxiId}">
                ${taxi.taxiId}
             </option>`;
    });

    dropdown.disabled =
        taxis.length === 0;
}

async function submitJob()
{

    const fromLoc = document.getElementById("fromInput").value;
    const toLoc = document.getElementById("toInput").value;
    const taxiId = document.getElementById("taxiDropdown").value;
    const response =
        await fetch(
            `${API_BASE_URL}/jobs`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    fromLoc,
                    toLoc,
                    taxiId
                })
            }
        );

    if (response.status === 201) 
    {
        alert("Job Created");

    }
    else 
    {
        const error = await response.json();
        alert(error.error);

    }
}

async function loadTaxiState()
{

    const response =
        await fetch(
            `${API_BASE_URL}/taxis`
        );

    const taxis =
        await response.json();

    const tbody =
        document.querySelector(
            "#taxiTable tbody"
        );

    tbody.innerHTML="";

    taxis.forEach(taxi=>{

        tbody.innerHTML += `
        <tr>
            <td>${taxi.taxiId}</td>
            <td>${taxi.status}</td>
            <td>${taxi.driverName}</td>
            <td>${taxi.jobId }</td>
        </tr>
        `;

    });
}

async function loadHistory(){

    const response =
        await fetch(
            `${API_BASE_URL}/history`
        );

    const jobs =
        await response.json();

    const tbody =
        document.querySelector(
            "#historyTable tbody"
        );

    tbody.innerHTML="";

    jobs.forEach(job=>{

        tbody.innerHTML += `
        <tr>
            <td>${job.jobId}</td>
            <td>${job.fromLocLocLoc}</td>
            <td>${job.status}</td>
            <td>${job.toLocLoc}</td>
            <td>${job.taxiId ?? "-"}</td>
            <td>${job.closedAt}</td>
        </tr>
        `;

    });
}



