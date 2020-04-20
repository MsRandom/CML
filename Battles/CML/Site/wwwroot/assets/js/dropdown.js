function showUsers() {
    document.querySelector("#users").removeAttribute("hidden");
}

function hideUsers() {
    document.querySelector("#users").setAttribute("hidden", "hidden");
}

function filterUsers(event) {
    if (event.code !== "Enter") {
        const input = document.querySelector("#user-filter");
        const div = document.querySelector("#users");
        const users = div.querySelectorAll("#users div");
        const text = input.value + event.key;
        users.forEach(user => {
            if (!user.innerHTML.includes("Username") && (!text || user.className.includes("selected") || user.innerHTML.toLowerCase().includes(text.toLowerCase())))
                user.style.display = "block";
            else
                user.style.display = "none";
        });
    }
}

function keyControl(e) {
    const element = document.querySelector("#users");
    if (!element.hidden) {
        const filter = document.querySelector("#user-filter");
        if (e.code === "ArrowDown") {
            let index = indexOfChild(element);
            if (index < element.childNodes.length - 1) {
                setSelected(element, filter, element.childNodes[index + (index === 0 ? 2 : 1)], element.childNodes[index]);
            }
        } else if (e.code === "ArrowUp") {
            let index = indexOfChild(element);
            if (index > 2) {
                setSelected(element, filter, element.childNodes[index - 1], element.childNodes[index]);
            }
        } else if (e.code === "Enter") {
            hideUsers()
        }
    }
}

function indexOfChild(users) {
    for (let i = 0; i < users.childNodes.length; i++) {
        if (users.childNodes[i].className && users.childNodes[i].className.includes("selected")) return i;
    }
    return -1;
}

function setSelected(element, filter, child, old) {
    if (old) {
        old.className = "";
    } else {
        if (element.className) document.getElementById(element.className).className = "";
        else element.firstElementChild.className = "";
    }
    if (child.id) {
        element.className = child.id;
        filter.placeholder = child.innerHTML;
    }
    filter.value = "";
    child.className = "selected";
}

async function setUsers(container) {
    const filter = document.querySelector("#user-filter");
    const element = document.querySelector("#users");
    filter.addEventListener("keypress", filterUsers);
    container.addEventListener("keyup", keyControl);
    const resp = await fetch("getUsers", {
        method: "get",
        headers: {
            "Content-Type": "application/json"
        }
    });
    const result = await resp.json();
    result.sort((a, b) => a.name > b.name ? 1 : a.name < b.name ? -1 : 0);
    for (const user of result) {
        const child = document.createElement("DIV");
        child.id = user.user;
        child.innerHTML = user.name;
        child.addEventListener("click", () => {
            setSelected(element, filter, child);
            hideUsers();
        });
        element.appendChild(child);
    }
}
