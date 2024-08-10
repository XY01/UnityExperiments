let socket;

const login = () => {
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;

    if (username === 'admin' && password === 'password') {
        document.getElementById('login').style.display = 'none';
        document.getElementById('dashboard').style.display = 'block';
        connectWebSocket();
        console.log('Login successful');
    } else {
        document.getElementById('login-error').textContent = 'Invalid username or password';
        document.getElementById('login-error').classList.remove('hidden');
    }
};

const loadUIElements = (elements) => {
    const controlsDiv = document.getElementById('controls');
    controlsDiv.innerHTML = ''; // Clear existing controls
    
    elements.forEach(element => {
        if (element.type === 'button') {
            const button = document.createElement('button');
            button.innerText = element.label;
            button.onclick = () => handleControlChange('button_click', element.label);
            controlsDiv.appendChild(button);
        } else if (element.type === 'slider') {
            const sliderContainer = document.createElement('div');
            sliderContainer.style.width = '100%';
            sliderContainer.style.marginBottom = '1rem';

            const label = document.createElement('label');
            label.innerText = `${element.label}: ${element.value}`;
            label.style.display = 'block';
            label.style.marginBottom = '0.5rem';

            const slider = document.createElement('input');
            slider.type = 'range';
            slider.min = element.min;
            slider.max = element.max;
            slider.value = element.value;
            slider.style.width = '100%';
            slider.oninput = (e) => {
                const newValue = e.target.value;
                label.innerText = `${element.label}: ${newValue}`;
                handleControlChange('slider_change', { ...element, value: newValue });
            };

            sliderContainer.appendChild(label);
            sliderContainer.appendChild(slider);
            controlsDiv.appendChild(sliderContainer);
        }
    });
};

const handleControlChange = (type, data) => {
    console.log(`Control change: ${type}`, data);
    if (socket && socket.readyState === WebSocket.OPEN) {
        const message = JSON.stringify({
            type: type,
            data: JSON.stringify(data)  // Stringify the data object
        });
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
        if (data.type === 'ui_update') {
            loadUIElements(JSON.parse(data.data));
        }
    };

    socket.onclose = () => {
        console.log('WebSocket disconnected');
        // Attempt to reconnect after a delay
        setTimeout(connectWebSocket, 5000);
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