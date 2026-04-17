import React from 'react';
import UploadTest from "./UploadTest";
import { Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import Home from './Pages/Home';
const App = () => {
    return (
        <Layout>
            <Routes>
                <Route path='/' element={<Home />} />
                <Route path="/upload-test" element={<UploadTest />} />
            </Routes>
        </Layout>
    );
}

export default App;