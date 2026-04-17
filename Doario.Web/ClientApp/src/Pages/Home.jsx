import React, {useState} from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import './Home.css';

const Home = () => {

    const [count, setCount] = useState(0);

    const onButtonClick = () => {
        setCount(count + 1);
    }
    
    return (
        <div className="app-container">
            <div className="d-flex flex-column justify-content-center align-items-center">
                <h1>Welcome to React</h1>
                <button onClick={onButtonClick} className="btn btn-primary mb-3">Click me</button>
                <h2>{count}</h2>
            </div>
        </div>
    );
};

export default Home;