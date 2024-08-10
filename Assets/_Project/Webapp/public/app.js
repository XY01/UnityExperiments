let socket;

const login = () => {
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;

    // For this example, we'll use a simple check. In a real app, this should be handled securely on the server.
    if (username === 'admin' && password === 'password') {
        document.getElementById('login').style.display = 'none';
        document.getElementById('dashboard').style.display = 'block';
        loadUIElements();
        connectWebSocket();
        console.log('Login successful');
    } else {
        document.getElementById('login-error').textContent = 'Invalid username or password';
        document.getElementById('login-error').classList.remove('hidden');
    }

   
};

const loadUIElements = () => {
    fetch('/api/ui-elements')
    .then(response => response.json())
    .then(elements => {
        const controlsDiv = document.getElementById('controls');
        controlsDiv.innerHTML = ''; // Clear existing controls
        
        elements.forEach(element => {
            if (element.type === 'button') {
                const button = document.createElement('button');
                button.innerText = element.label;
                button.onclick = () => handleControlChange(element.id, 'button_click');
                controlsDiv.appendChild(button);
            } else if (element.type === 'slider') {
                const sliderContainer = document.createElement('div');
                sliderContainer.style.width = '100%';
                sliderContainer.style.marginBottom = '1rem';

                const label = document.createElement('label');
                label.innerText = element.label;
                label.style.display = 'block';
                label.style.marginBottom = '0.5rem';

                const slider = document.createElement('input');
                slider.type = 'range';
                slider.min = element.min;
                slider.max = element.max;
                slider.value = element.value;
                slider.style.width = '100%';
                slider.oninput = () => handleControlChange(element.id, slider.value);

                sliderContainer.appendChild(label);
                sliderContainer.appendChild(slider);
                controlsDiv.appendChild(sliderContainer);
            }
        });
    })
    .catch(error => {
        console.error('Error loading UI elements:', error);
        document.getElementById('controls').innerHTML = 'Error loading controls. Please try again.';
    });
};

const handleControlChange = (id, value) => {
    console.log(`Control ${id} changed to ${value}`);
    if (socket && socket.readyState === WebSocket.OPEN) {
        const message = JSON.stringify({ id, value });
        console.log('Sending message:', message);
        socket.send(message);
    } else {
        console.error('WebSocket is not connected');
    }
};

const connectWebSocket = () => {
    socket = new WebSocket(`ws://${window.location.host}`);

    socket.onopen = () => {
        console.log('WebSocket connected');
    };

    socket.onmessage = (event) => {
        const data = JSON.parse(event.data);
        console.log('Received WebSocket message:', data);
        // Handle incoming WebSocket messages if needed
    };

    socket.onclose = () => {
        console.log('WebSocket disconnected');
    };

    socket.onerror = (error) => {
        console.error('WebSocket error:', error);
    };
};

// Attach the login function to the form submit event
document.getElementById('login-form').onsubmit = function(e) {
    e.preventDefault();
    login();
};