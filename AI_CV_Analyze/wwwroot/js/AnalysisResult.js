// ================ PHÂN TÍCH KẾT QUẢ CV - JAVASCRIPT ================
// File này xử lý tất cả logic tương tác và animation cho trang AnalysisResult
// Bao gồm: CV Scoring, Job Recommendation, Modal, Loading, và các hiệu ứng UI
// 
// CÁC CHỨC NĂNG CHÍNH:
// 1. Animation scroll - Hiệu ứng xuất hiện khi cuộn trang
// 2. CV Scoring - Chấm điểm CV với AI và hiệu ứng animation
// 3. Job Recommendation - Gợi ý công việc phù hợp
// 4. Modal Management - Quản lý các modal (loading, chia sẻ, chi tiết)
// 5. Form Handling - Xử lý form gửi gợi ý chỉnh sửa
// 6. Toast Notifications - Thông báo popup nhỏ
// 7. Copy Functionality - Sao chép nội dung vào clipboard
// 8. Enhanced UI Effects - Hiệu ứng hover, click, particle

// Global jobRecommendHandler variable
let jobRecommendHandler;

document.addEventListener('DOMContentLoaded', function () {
    

    
    // ================ XỬ LÝ GỢI Ý CÔNG VIỆC ================
    // Quản lý việc gợi ý công việc phù hợp dựa trên CV
    // Mục đích: Sử dụng AI để phân tích kỹ năng và kinh nghiệm, sau đó gợi ý công việc phù hợp
    jobRecommendHandler = {
        // Các element cần thiết cho job recommendation
        // Lưu trữ tất cả các element DOM cần thiết để tránh query nhiều lần
        elements: {
            btnJobRecommend: document.getElementById('btnJobRecommend'), // Nút gợi ý công việc (trạng thái ban đầu)
            btnRetryJobRecommend: document.getElementById('btnRetryJobRecommend'), // Nút thử lại (khi có lỗi)
            jobInitialState: document.getElementById('jobInitialState'), // Trạng thái ban đầu (hiển thị nút "Get Recommendations")
            jobLoadingState: document.getElementById('jobLoadingState'), // Trạng thái loading (spinner + text)
            jobRecommendResult: document.getElementById('jobRecommendResult'), // Kết quả gợi ý từ API
            jobRecommendResultFromSession: document.getElementById('jobRecommendResultFromSession'), // Kết quả từ session (khi reload trang)
            jobErrorState: document.getElementById('jobErrorState') // Trạng thái lỗi (thông báo lỗi)
        },
        abortController: null, // AbortController để hủy request khi cần

        // Hiển thị trạng thái cụ thể và ẩn các trạng thái khác
        // Mục đích: Quản lý việc chuyển đổi giữa các trạng thái UI (initial, loading, result, error)
        showState: function(stateName) {
            // Ẩn tất cả các trạng thái trước khi hiển thị trạng thái mới
            Object.values(this.elements).forEach(element => {
                if (element && element.style) {
                    element.style.display = 'none'; // Ẩn tất cả element
                }
            });
            
            // Hiển thị trạng thái được yêu cầu
            if (this.elements[stateName]) {
                this.elements[stateName].style.display = 'block'; // Hiển thị element tương ứng
            }
        },

        // Bắt đầu loading - tạo controller mới và hiển thị trạng thái loading
        // Mục đích: Chuẩn bị cho việc gửi request và hiển thị loading state
        startLoading: function() {
            this.abortController = new AbortController(); // Tạo controller mới để có thể hủy request
            this.showState('jobLoadingState'); // Hiển thị trạng thái loading (spinner + text)
        },

        // Hiển thị lỗi - hiển thị trạng thái lỗi và log lỗi
        // Mục đích: Xử lý và hiển thị lỗi khi có vấn đề với request
        showError: function(message) {
            this.showState('jobErrorState'); // Hiển thị trạng thái lỗi cho người dùng
            console.error('Job recommendation error:', message); // Log lỗi để debug
        },

        // Hiển thị kết quả gợi ý công việc từ API
        // Mục đích: Render HTML cho kết quả gợi ý công việc với giao diện đẹp và thông tin chi tiết
        showResults: function(data) {
            this.showState('jobRecommendResult'); // Chuyển sang trạng thái hiển thị kết quả
            
            let html = ''; // Biến lưu HTML kết quả
            
            // Xử lý trường hợp có công việc được gợi ý (thành công)
            if (data && data.recommendedJob) {
                html = `
                    <div class="space-y-6">
                        <!-- Header với icon thành công -->
                        <div class="text-center mb-6">
                            <div class="w-16 h-16 mx-auto mb-4 bg-gradient-to-br from-green-400 to-green-600 rounded-full flex items-center justify-center">
                                <i class="fas fa-check text-white text-2xl"></i>
                            </div>
                            <h3 class="text-xl font-bold text-gray-800 mb-2">Perfect Match Found!</h3>
                            <p class="text-gray-600">Based on your skills and experience, we recommend:</p>
                        </div>

                        <!-- Job Card - Hiển thị công việc được gợi ý -->
                        <div class="bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-200 rounded-xl p-6 hover:shadow-lg transition-all duration-300">
                            <div class="flex items-start mb-4">
                                <div class="flex items-center">
                                    <div class="w-12 h-12 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-lg flex items-center justify-center mr-4">
                                        <i class="fas fa-briefcase text-white text-lg"></i>
                                    </div>
                                    <div>
                                        <h4 class="text-xl font-bold text-gray-900 mb-1">${data.recommendedJob}</h4>
                                        <p class="text-sm text-gray-600">Recommended for you</p>
                                    </div>
                                </div>
                            </div>

                            <!-- Skills to Improve - Hiển thị kỹ năng cần cải thiện nếu có -->
                            ${data.missingSkills && data.missingSkills.length > 0 ? `
                                <div class="bg-gradient-to-br from-yellow-50 to-orange-50 border border-yellow-200 rounded-xl p-4">
                                    <div class="flex items-center mb-3">
                                        <div class="w-8 h-8 bg-gradient-to-br from-yellow-500 to-orange-500 rounded-lg flex items-center justify-center mr-3">
                                            <i class="fas fa-lightbulb text-white text-sm"></i>
                                        </div>
                                        <h5 class="text-sm font-semibold text-yellow-800">Skills to Improve</h5>
                                    </div>
                                    <div class="flex flex-wrap gap-2">
                                        ${data.missingSkills.map(skill => `
                                            <span class="bg-gradient-to-r from-yellow-100 to-orange-100 text-yellow-800 px-3 py-1 rounded-full text-xs font-medium border border-yellow-200 hover:from-yellow-200 hover:to-orange-200 transition-all duration-200">
                                                ${skill}
                                            </span>
                                        `).join('')}
                                    </div>
                                </div>
                            ` : ''}
                        </div>

                        <!-- Action Buttons - Nút hành động -->
                        <div class="flex flex-col sm:flex-row gap-2">
                            <a href="/CVAnalysis/JobPrediction" class="group flex-1 flex justify-center items-center px-4 py-2 border border-gray-300 rounded-lg shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-gray-300 transition-all duration-300 transform hover:-translate-y-0.5 hover:shadow-md">
                                <i class="fas fa-search mr-2 group-hover:animate-bounce"></i>View More Jobs
                            </a>
                        </div>
                    </div>
                `;
            } 
            // Xử lý trường hợp cần cải thiện (AI phát hiện cần bổ sung thêm thông tin)
            else if (data && data.improvementPlan) {
                html = `
                    <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-6">
                        <div class="flex items-start">
                            <i class="fas fa-exclamation-triangle text-yellow-500 mt-1 mr-3"></i>
                            <div>
                                <h4 class="text-sm font-semibold text-yellow-800 mb-2">Improvement Needed</h4>
                                <p class="text-sm text-yellow-700">${data.improvementPlan}</p>
                            </div>
                        </div>
                    </div>
                `;
            } 
            // Xử lý trường hợp không tìm thấy gợi ý (AI không thể tìm thấy công việc phù hợp)
            else {
                html = `
                    <div class="text-center py-8">
                        <div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
                            <i class="fas fa-info-circle text-gray-400 text-xl"></i>
                        </div>
                        <h4 class="text-lg font-semibold text-gray-800 mb-2">No Recommendations Found</h4>
                        <p class="text-sm text-gray-600">We couldn't find suitable job matches for your current profile.</p>
                    </div>
                `;
            }
            
            this.elements.jobRecommendResult.innerHTML = html; // Cập nhật nội dung HTML cho element kết quả
        },

        // Hiển thị kết quả từ session (khi trang load với dữ liệu có sẵn)
        // Mục đích: Hiển thị lại kết quả gợi ý công việc khi người dùng reload trang hoặc quay lại
        showResultsFromSession: function(data) {
            if (this.elements.jobRecommendResultFromSession) {
                this.elements.jobRecommendResultFromSession.style.display = 'block'; // Hiển thị element
                
                let html = ''; // Biến lưu HTML kết quả
                
                // Xử lý trường hợp có công việc được gợi ý (giống như showResults nhưng cho session)
                if (data && data.recommendedJob) {
                    html = `
                        <div class="space-y-6">
                            <div class="text-center mb-6">
                                <div class="w-16 h-16 mx-auto mb-4 bg-gradient-to-br from-green-400 to-green-600 rounded-full flex items-center justify-center">
                                    <i class="fas fa-check text-white text-2xl"></i>
                                </div>
                                <h3 class="text-xl font-bold text-gray-800 mb-2">Perfect Match Found!</h3>
                                <p class="text-gray-600">Based on your skills and experience, we recommend:</p>
                            </div>

                            <!-- Job Card -->
                            <div class="bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-200 rounded-xl p-6 hover:shadow-lg transition-all duration-300">
                                <div class="flex items-start mb-4">
                                    <div class="flex items-center">
                                        <div class="w-12 h-12 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-lg flex items-center justify-center mr-4">
                                            <i class="fas fa-briefcase text-white text-lg"></i>
                                        </div>
                                        <div>
                                            <h4 class="text-xl font-bold text-gray-900 mb-1">${data.recommendedJob}</h4>
                                            <p class="text-sm text-gray-600">Recommended for you</p>
                                        </div>
                                    </div>
                                </div>

                                <!-- Skills to Improve -->
                                ${data.missingSkills && data.missingSkills.length > 0 ? `
                                    <div class="bg-gradient-to-br from-yellow-50 to-orange-50 border border-yellow-200 rounded-xl p-4">
                                        <div class="flex items-center mb-3">
                                            <div class="w-8 h-8 bg-gradient-to-br from-yellow-500 to-orange-500 rounded-lg flex items-center justify-center mr-3">
                                                <i class="fas fa-lightbulb text-white text-sm"></i>
                                            </div>
                                            <h5 class="text-sm font-semibold text-yellow-800">Skills to Improve</h5>
                                        </div>
                                        <div class="flex flex-wrap gap-2">
                                            ${data.missingSkills.map(skill => `
                                                <span class="bg-gradient-to-r from-yellow-100 to-orange-100 text-yellow-800 px-3 py-1 rounded-full text-xs font-medium border border-yellow-200 hover:from-yellow-200 hover:to-orange-200 transition-all duration-200">
                                                    ${skill}
                                                </span>
                                            `).join('')}
                                        </div>
                                    </div>
                                ` : ''}
                            </div>

                            <!-- Action Buttons -->
                            <div class="flex flex-col sm:flex-row gap-2">
                                <a href="/CVAnalysis/JobPrediction" class="group flex-1 flex justify-center items-center px-4 py-2 border border-gray-300 rounded-lg shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-gray-300 transition-all duration-300 transform hover:-translate-y-0.5 hover:shadow-md">
                                    <i class="fas fa-search mr-2 group-hover:animate-bounce"></i>View More Jobs
                                </a>
                            </div>
                        </div>
                    `;
                } 
                // Xử lý trường hợp cần cải thiện (AI phát hiện cần bổ sung thêm thông tin)
                else if (data && data.improvementPlan) {
                    html = `
                        <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-6">
                            <div class="flex items-start">
                                <i class="fas fa-exclamation-triangle text-yellow-500 mt-1 mr-3"></i>
                                <div>
                                    <h4 class="text-sm font-semibold text-yellow-800 mb-2">Improvement Needed</h4>
                                    <p class="text-sm text-yellow-700">${data.improvementPlan}</p>
                                </div>
                            </div>
                        </div>
                    `;
                } 
                // Xử lý trường hợp không tìm thấy gợi ý (AI không thể tìm thấy công việc phù hợp)
                else {
                    html = `
                        <div class="text-center py-8">
                            <div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
                                <i class="fas fa-info-circle text-gray-400 text-xl"></i>
                            </div>
                            <h4 class="text-lg font-semibold text-gray-800 mb-2">No Recommendations Found</h4>
                            <p class="text-sm text-gray-600">We couldn't find suitable job matches for your current profile.</p>
                        </div>
                    `;
                }
                
                this.elements.jobRecommendResultFromSession.innerHTML = html; // Cập nhật nội dung HTML cho element session
            }
        },

        // Xử lý click nút gợi ý công việc
        // Mục đích: Thu thập dữ liệu CV và gửi lên server để AI phân tích và gợi ý công việc
        handleClick: function() {
            // Lấy kỹ năng và kinh nghiệm làm việc từ các trường ẩn trong form
            // Các trường này được điền tự động từ kết quả phân tích CV trước đó
            const skills = document.getElementById('cvSkills').value || ''; // Kỹ năng được trích xuất
            const experience = document.getElementById('cvExperience').value || ''; // Kinh nghiệm làm việc
            const project = document.getElementById('cvProject').value || ''; // Dự án đã thực hiện
            
            // Kết hợp kinh nghiệm và dự án thành một chuỗi duy nhất
            // Để AI có thể phân tích toàn bộ thông tin làm việc
            let workExperience = '';
            if (experience && project) {
                workExperience = experience + '\n\n' + project; // Kết hợp cả hai với dòng trống ở giữa
            } else if (experience) {
                workExperience = experience; // Chỉ có kinh nghiệm làm việc
            } else if (project) {
                workExperience = project; // Chỉ có dự án (có thể là sinh viên mới tốt nghiệp)
            }
            
            this.startLoading(); // Bắt đầu loading state
            
            // Gửi request đến server để lấy gợi ý công việc từ AI
            const csrfToken = document.querySelector('input[name=__RequestVerificationToken]')?.value || '';
            
            fetch('/CVAnalysis/RecommendJobs', {
                method: 'POST', // Phương thức POST để gửi dữ liệu
                headers: {
                    'Content-Type': 'application/json', // Định dạng JSON
                    'RequestVerificationToken': csrfToken // Token bảo mật chống CSRF
                },
                body: JSON.stringify({ 
                    skills: skills, // Kỹ năng được trích xuất từ CV
                    workExperience: workExperience // Kinh nghiệm làm việc và dự án
                }),
                signal: this.abortController.signal // Signal để có thể hủy request khi cần
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json(); // Parse response thành JSON object
            })
            .then(data => {
                this.showResults(data); // Hiển thị kết quả gợi ý công việc
            })
            .catch((err) => {
                // Xử lý các loại lỗi khác nhau
                if (err.name === 'AbortError') {
                    this.showError('Request was cancelled'); // Thông báo đã hủy (người dùng ấn cancel)
                } else {
                    this.showError('Error fetching recommendations: ' + err.message); // Thông báo lỗi khác (mạng, server, v.v.)
                }
            });
        },

        // Khởi tạo job recommendation handler và gắn các event listener
        init: function() {
            // Gắn event listener cho nút gợi ý công việc (trạng thái ban đầu)
            if (this.elements.btnJobRecommend) {
                this.elements.btnJobRecommend.addEventListener('click', () => {
                    this.handleClick();
                });
            } else {
                // Try to find it again
                const btn = document.getElementById('btnJobRecommend');
                if (btn) {
                    btn.addEventListener('click', () => {
                        this.handleClick();
                    });
                }
            }
            
            // Gắn event listener cho nút thử lại (khi có lỗi)
            if (this.elements.btnRetryJobRecommend) {
                this.elements.btnRetryJobRecommend.addEventListener('click', () => this.handleClick());
            }

            // Kiểm tra xem có gợi ý công việc từ session không và hiển thị chúng
            // Để người dùng không mất kết quả khi reload trang
            const jobRecommendationsFromSession = document.getElementById('jobRecommendationsFromSession');
            if (jobRecommendationsFromSession && jobRecommendationsFromSession.value) {
                try {
                    const sessionData = JSON.parse(jobRecommendationsFromSession.value); // Parse JSON từ session
                    this.showResultsFromSession(sessionData); // Hiển thị kết quả từ session
                } catch (error) {
                    console.error('Error parsing job recommendations from session:', error); // Log lỗi nếu parse thất bại
                }
            }
        }
    };
    
    // Initialize jobRecommendHandler immediately
    jobRecommendHandler.init();

    // Direct test for job recommendation button
    const btnJobRecommendDirect = document.getElementById('btnJobRecommend');
    if (btnJobRecommendDirect) {
        btnJobRecommendDirect.addEventListener('click', function() {
            setTimeout(() => {
                if (typeof jobRecommendHandler !== 'undefined' && jobRecommendHandler && jobRecommendHandler.handleClick) {
                    jobRecommendHandler.handleClick();
                }
            }, 100);
        });
    }

    // ================ HỆ THỐNG ANIMATION SCROLL ================
    // Hiệu ứng xuất hiện các phần tử khi cuộn trang (từ Home.js)
    // Mục đích: Tạo hiệu ứng fade-in cho các element khi chúng xuất hiện trong viewport
    const animateOnScroll = function () {
        // Tìm tất cả các phần tử có class animation (fade-in từ các hướng khác nhau)
        const elements = document.querySelectorAll('.animate-fade-in-up, .animate-fade-in-right, .animate-fade-in-left, .animate-fade-in');

        elements.forEach(element => {
            // Lấy vị trí của phần tử so với viewport (khoảng cách từ đỉnh màn hình)
            const elementPosition = element.getBoundingClientRect().top;
            const windowHeight = window.innerHeight; // Chiều cao của cửa sổ trình duyệt

            // Kiểm tra nếu phần tử nằm trong vùng hiển thị (cách đỉnh 100px để tạo khoảng đệm)
            if (elementPosition < windowHeight - 100) {
                // Hiển thị phần tử với opacity = 1 và transform về vị trí gốc
                element.style.opacity = '1';
                element.style.transform = 'translate(0)';
            }
        });
    };

    // Chạy animation ngay khi trang load để hiển thị các element có sẵn
    animateOnScroll();

    // Chạy animation khi cuộn trang để hiển thị các element mới xuất hiện
    window.addEventListener('scroll', animateOnScroll);

    // ================ HIỆU ỨNG HOVER NÂNG CAO ================
    // Thêm hiệu ứng hover cho tất cả button và link để tăng trải nghiệm người dùng
    // Mục đích: Tạo phản hồi trực quan khi người dùng di chuột qua các element tương tác
    document.querySelectorAll('button, a').forEach(button => {
        // Khi di chuột vào - phóng to element lên 105%
        button.addEventListener('mouseenter', () => {
            button.classList.add('hover:scale-105'); // Phóng to 105% khi hover
        });
        // Khi di chuột ra - trở về kích thước ban đầu
        button.addEventListener('mouseleave', () => {
            button.classList.remove('hover:scale-105'); // Trở về kích thước ban đầu
        });
    });

    // ================ CHỨC NĂNG COPY NÂNG CAO ================
    // Xử lý copy nội dung gợi ý với hiệu ứng animation
    // Mục đích: Cho phép người dùng sao chép nhanh các gợi ý AI vào clipboard với phản hồi trực quan
    document.getElementById('copy-btn')?.addEventListener('click', function() {
        // Lấy nội dung text từ phần gợi ý AI (từ element có class ai-suggestion-text)
        const suggestionsText = document.querySelector('.ai-suggestion-text').innerText;
        
        // Sử dụng Clipboard API để copy nội dung vào clipboard
        navigator.clipboard.writeText(suggestionsText).then(() => {
            const copyBtn = document.getElementById('copy-btn');
            
            // Thay đổi giao diện button thành trạng thái "đã copy" với màu xanh và icon check
            copyBtn.innerHTML = '<i class="fas fa-check mr-2"></i> Copied!';
            copyBtn.classList.remove('bg-blue-600', 'hover:bg-blue-700'); // Xóa màu xanh dương
            copyBtn.classList.add('bg-green-600', 'hover:bg-green-700'); // Thêm màu xanh lá
            
            // Sau 2 giây, trở về trạng thái ban đầu với icon copy và màu xanh dương
            setTimeout(() => {
                copyBtn.innerHTML = '<i class="far fa-copy mr-2"></i> Copy';
                copyBtn.classList.remove('bg-green-600', 'hover:bg-green-700'); // Xóa màu xanh lá
                copyBtn.classList.add('bg-blue-600', 'hover:bg-blue-700'); // Thêm lại màu xanh dương
            }, 2000);
        });
    });

    // ================ MODAL CHIA SẺ NÂNG CAO ================
    // Xử lý modal chia sẻ kết quả phân tích CV
    // Mục đích: Cho phép người dùng chia sẻ kết quả phân tích CV với người khác thông qua link
    
    // Hiển thị modal chia sẻ khi click nút Share
    // Modal sẽ chứa link chia sẻ và các tùy chọn chia sẻ khác
    document.getElementById('shareResultBtn')?.addEventListener('click', function() {
        document.getElementById('shareModal').classList.remove('hidden'); // Hiển thị modal
    });

    // Ẩn modal chia sẻ khi click nút đóng (X)
    // Người dùng có thể đóng modal để quay lại trang chính
    document.getElementById('closeShareModal')?.addEventListener('click', function() {
        document.getElementById('shareModal').classList.add('hidden'); // Ẩn modal
    });

    // Copy link chia sẻ vào clipboard khi click nút "Copy Link"
    // Sử dụng phương pháp cũ (execCommand) để tương thích với nhiều trình duyệt
    document.getElementById('copyShareLink')?.addEventListener('click', function() {
        const shareLink = document.getElementById('shareLink'); // Lấy element chứa link
        shareLink.select(); // Chọn toàn bộ text trong input
        document.execCommand('copy'); // Copy vào clipboard (phương pháp cũ nhưng tương thích tốt)
        
        // Hiển thị thông báo copy thành công để người dùng biết đã copy thành công
        const copySuccess = document.getElementById('copySuccess');
        if (copySuccess) {
            copySuccess.classList.remove('hidden'); // Hiển thị thông báo
            
            // Tự động ẩn thông báo sau 2 giây để không làm rối mắt
            setTimeout(() => {
                copySuccess.classList.add('hidden'); // Ẩn thông báo
            }, 2000);
        }
    });

    // ================ MODAL CHI TIẾT NỘI DUNG NÂNG CAO ================
    // Xử lý modal hiển thị chi tiết nội dung CV
    // Mục đích: Cho phép người dùng xem nội dung đầy đủ của các phần CV khi nội dung bị cắt ngắn
    
    // Xử lý click vào nút "View Details" để hiển thị modal chi tiết
    // Khi nội dung CV quá dài, chỉ hiển thị một phần và có nút "View Details" để xem đầy đủ
    document.querySelectorAll('.show-more-btn').forEach(button => {
        button.addEventListener('click', function() {
            const card = this.closest('.content-card'); // Tìm card chứa nút (tìm phần tử cha gần nhất có class content-card)
            const title = card.getAttribute('data-title'); // Lấy tiêu đề từ data attribute (ví dụ: "Kinh nghiệm làm việc")
            const content = card.getAttribute('data-full-content'); // Lấy nội dung đầy đủ từ data attribute
            
            // Cập nhật nội dung modal với tiêu đề và nội dung đầy đủ
            document.getElementById('modalTitle').textContent = title; // Cập nhật tiêu đề modal
            document.getElementById('modalContent').innerHTML = content; // Cập nhật nội dung modal
            document.getElementById('contentDetailModal').classList.remove('hidden'); // Hiển thị modal
        });
    });

    // Ẩn modal khi click nút đóng (X) hoặc click bên ngoài modal
    // Cho phép người dùng đóng modal để quay lại trang chính
    document.getElementById('closeContentModal')?.addEventListener('click', function() {
        document.getElementById('contentDetailModal').classList.add('hidden'); // Ẩn modal
    });

    // ================ ANIMATION CHẤM ĐIỂM CV NÂNG CAO ================
    // Xử lý chấm điểm CV với giao diện mới và hiệu ứng animation
    // Mục đích: Sử dụng AI để chấm điểm CV theo các tiêu chí khác nhau với hiệu ứng animation mượt mà
    
    // Xử lý sự kiện click nút "Start AI Scoring"
    // Khi người dùng click nút này, hệ thống sẽ gửi CV lên server để AI phân tích và chấm điểm
    document.getElementById('btnScoreCV')?.addEventListener('click', function() {
        // Lấy ID của CV từ data attribute để server biết chấm điểm CV nào
        const resumeId = this.getAttribute('data-resumeid');
        
        // Lấy các element cần thiết cho việc hiển thị trạng thái và kết quả
        const scoreTable = document.getElementById('cv-score-table'); // Bảng hiển thị điểm số
        const scoreLoading = document.getElementById('score-loading'); // Trạng thái loading nhỏ
        const scoreError = document.getElementById('score-error'); // Thông báo lỗi nếu có
        
        // Lấy nội dung CV để kiểm tra validation
        const cvContentInput = document.querySelector('input[name="Content"]');
        const cvContent = cvContentInput ? cvContentInput.value : '';
        

        
        // KIỂM TRA VALIDATION: Kiểm tra xem có CV để chấm điểm không
        // Lưu ý: resumeId có thể = 0 cho guest users, nhưng vẫn có thể chấm điểm nếu có nội dung CV
        // Chỉ kiểm tra resumeId nếu nó không phải là "0" (guest users)
        if (resumeId !== '0' && (!resumeId || resumeId.trim() === '')) {
            // Hiển thị lỗi ngay lập tức nếu không có CV
            if (scoreError) {
                scoreError.style.display = 'block';
                scoreError.innerHTML = `
                    <div class="bg-red-50 border border-red-200 rounded-lg p-4">
                        <div class="flex items-start">
                            <i class="fas fa-exclamation-circle text-red-500 mt-1 mr-3"></i>
                            <div>
                                <h4 class="text-sm font-semibold text-red-800 mb-2">No CV Found</h4>
                                <p class="text-sm text-red-700">Please upload and analyze a CV first before scoring.</p>
                            </div>
                        </div>
                    </div>
                `;
                
                // Tự động ẩn lỗi sau 5 giây với hiệu ứng fade out
                setTimeout(() => {
                    if (scoreError) {
                        // Thêm hiệu ứng fade out
                        scoreError.style.transition = 'opacity 0.5s ease-out';
                        scoreError.style.opacity = '0';
                        
                        // Ẩn hoàn toàn sau khi fade out xong
                        setTimeout(() => {
                            if (scoreError) {
                                scoreError.style.display = 'none';
                                scoreError.style.opacity = '1'; // Reset opacity cho lần sau
                            }
                        }, 500);
                    }
                }, 5000);
            }
            // Hiển thị toast notification
            if (typeof toast !== 'undefined') {
                toast.show('Please upload and analyze a CV first before scoring.', 'error');
            }
            return; // Dừng xử lý ngay lập tức
        }
        
        // KIỂM TRA THÊM: Kiểm tra xem có nội dung CV không
        if (!cvContent || cvContent.trim() === '') {
            // Hiển thị lỗi ngay lập tức nếu không có nội dung CV
            if (scoreError) {
                scoreError.style.display = 'block';
                scoreError.innerHTML = `
                    <div class="bg-red-50 border border-red-200 rounded-lg p-4">
                        <div class="flex items-start">
                            <i class="fas fa-exclamation-circle text-red-500 mt-1 mr-3"></i>
                            <div>
                                <h4 class="text-sm font-semibold text-red-800 mb-2">No CV Content Found</h4>
                                <p class="text-sm text-red-700">CV content is empty. Please upload a valid CV first.</p>
                            </div>
                        </div>
                    </div>
                `;
                
                // Tự động ẩn lỗi sau 5 giây với hiệu ứng fade out
                setTimeout(() => {
                    if (scoreError) {
                        // Thêm hiệu ứng fade out
                        scoreError.style.transition = 'opacity 0.5s ease-out';
                        scoreError.style.opacity = '0';
                        
                        // Ẩn hoàn toàn sau khi fade out xong
                        setTimeout(() => {
                            if (scoreError) {
                                scoreError.style.display = 'none';
                                scoreError.style.opacity = '1'; // Reset opacity cho lần sau
                            }
                        }, 500);
                    }
                }, 5000);
            }
            // Hiển thị toast notification
            if (typeof toast !== 'undefined') {
                toast.show('CV content is empty. Please upload a valid CV first.', 'error');
            }
            return; // Dừng xử lý ngay lập tức
        }
        
        // Ẩn nút chấm điểm và hiển thị trạng thái loading
        this.style.display = 'none'; // Ẩn nút "Start AI Scoring" để tránh click nhiều lần
        scoreError.style.display = 'none'; // Ẩn thông báo lỗi cũ nếu có
        scoreLoading.style.display = 'block'; // Hiển thị trạng thái loading (spinner + text)
        
        // Sử dụng nội dung CV đã lấy ở trên để gửi lên server
        // Nội dung CV được lưu trong input ẩn để tránh hiển thị quá dài trên giao diện
        
        // Gửi request chấm điểm CV lên server thông qua API endpoint
        // Server sẽ sử dụng AI để phân tích và chấm điểm CV theo các tiêu chí
        fetch('/CVAnalysis/ScoreCV', {
            method: 'POST', // Phương thức POST để gửi dữ liệu
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded', // Định dạng dữ liệu gửi lên
                'RequestVerificationToken': document.querySelector('input[name=__RequestVerificationToken]')?.value || '' // Token bảo mật chống CSRF
            },
            body: 'resumeId=' + encodeURIComponent(resumeId) + '&cvContent=' + encodeURIComponent(cvContent) // Dữ liệu gửi lên: ID CV và nội dung CV
        })
        .then(res => res.json()) // Chuyển response từ server thành JSON object
        .then(data => {
            // Ẩn loading sau khi nhận được kết quả từ server
            scoreLoading.style.display = 'none'; // Ẩn trạng thái loading nhỏ
            
            if (data.success) {
                // Nếu server trả về thành công, hiển thị bảng điểm với animation mượt mà
                scoreTable.style.display = 'block'; // Hiển thị bảng điểm
                scoreTable.classList.add('score-reveal'); // Thêm class để tạo hiệu ứng xuất hiện
                
                // Ẩn button "Start AI Scoring" sau khi chấm điểm thành công
                this.style.display = 'none';
                
                // Lưu trạng thái đã chấm điểm thành công vào sessionStorage
                // Để biết rằng CV này đã được chấm điểm trong session hiện tại
                const resumeId = this.getAttribute('data-resumeid');
                if (resumeId) {
                    sessionStorage.setItem('cv_scored_' + resumeId, 'true');
                }
                
                // Cập nhật từng điểm với animation riêng biệt theo thứ tự
                // Mỗi điểm sẽ có màu sắc khác nhau để dễ phân biệt
                updateScoreWithAnimation('score-layout', data.layout, 'blue'); // Điểm Layout (bố cục CV)
                updateScoreWithAnimation('score-skill', data.skill, 'green'); // Điểm Skills (kỹ năng)
                updateScoreWithAnimation('score-experience', data.experience, 'purple'); // Điểm Experience (kinh nghiệm)
                updateScoreWithAnimation('score-education', data.education, 'orange'); // Điểm Education (học vấn)
                updateScoreWithAnimation('score-keyword', data.keyword, 'pink'); // Điểm Keywords (từ khóa)
                updateScoreWithAnimation('score-format', data.format, 'teal'); // Điểm Format (định dạng)
                
                // Cập nhật tổng điểm sau 1 giây để tạo hiệu ứng delay và thu hút sự chú ý
                setTimeout(() => {
                    updateScoreWithAnimation('score-total', data.total, 'indigo'); // Tổng điểm (điểm quan trọng nhất)
                    updateScoreLevel(data.total); // Cập nhật mức độ đánh giá (Excellent/Good/Average/Needs Improvement)
                }, 1000);
                
                // Thêm animation thành công cho toàn bộ khối chấm điểm
                // Tạo hiệu ứng "hoàn thành" cho toàn bộ section
                const scoreBlock = document.getElementById('cv-score-block');
                scoreBlock.classList.add('score-loading-animation'); // Thêm animation
                setTimeout(() => {
                    scoreBlock.classList.remove('score-loading-animation'); // Xóa animation sau 2 giây
                }, 2000);
                
            } else {
                // Nếu server trả về lỗi, hiển thị thông báo lỗi và cho phép thử lại
                scoreError.style.display = 'block'; // Hiển thị thông báo lỗi
                this.style.display = 'block'; // Hiện lại nút chấm điểm để người dùng có thể thử lại
            }
        })
        .catch(error => {
            // Xử lý lỗi khi chấm điểm CV (lỗi mạng, server down, v.v.)
            console.error('CV Scoring error:', error); // Log lỗi để debug
            scoreLoading.style.display = 'none'; // Ẩn trạng thái loading
            scoreError.style.display = 'block'; // Hiển thị thông báo lỗi cho người dùng
            this.style.display = 'block'; // Hiện lại nút chấm điểm để người dùng có thể thử lại
        });
    });

    // ================ HÀM CẬP NHẬT ĐIỂM VỚI ANIMATION ================
    // Hàm cập nhật điểm với hiệu ứng animation mượt mà
    // Mục đích: Tạo hiệu ứng đếm số từ 0 đến điểm cuối cùng với animation mượt mà
    function updateScoreWithAnimation(elementId, finalScore, colorClass) {
        const element = document.getElementById(elementId); // Lấy element hiển thị điểm theo ID
        if (!element) return; // Thoát nếu không tìm thấy element (tránh lỗi)
        
        const progressBar = element.closest('.score-item').querySelector('.bg-gradient-to-r'); // Tìm thanh tiến trình trong cùng score-item
        const startScore = 0; // Điểm bắt đầu (luôn từ 0)
        const duration = 1500; // Thời gian animation (1.5 giây)
        const startTime = performance.now(); // Thời điểm bắt đầu animation (độ chính xác cao)
        
        // Hàm animation được gọi liên tục để cập nhật điểm số
        function animate(currentTime) {
            const elapsed = currentTime - startTime; // Thời gian đã trôi qua từ khi bắt đầu
            const progress = Math.min(elapsed / duration, 1); // Tiến độ animation (0-1), không vượt quá 1
            
            // Hàm easing để tạo animation mượt mà (easeOutQuart)
            // Tạo hiệu ứng chậm dần ở cuối thay vì tuyến tính
            const easeOutQuart = 1 - Math.pow(1 - progress, 4);
            const currentScore = Math.floor(easeOutQuart * (finalScore - startScore) + startScore); // Tính điểm hiện tại dựa trên tiến độ
            
            // Cập nhật text hiển thị điểm với format "điểm / điểm tối đa"
            element.textContent = `${currentScore} / ${elementId === 'score-total' ? '60' : '10'}`;
            
            // Cập nhật thanh tiến trình (progress bar) để hiển thị trực quan
            if (progressBar) {
                const percentage = (currentScore / (elementId === 'score-total' ? 60 : 10)) * 100; // Tính phần trăm hoàn thành
                progressBar.style.width = `${percentage}%`; // Cập nhật độ rộng thanh tiến trình
            }
            
            // Tiếp tục animation nếu chưa hoàn thành (progress < 1)
            if (progress < 1) {
                requestAnimationFrame(animate); // Gọi frame tiếp theo để tạo animation mượt mà
            } else {
                // Thêm hiệu ứng hoàn thành khi animation kết thúc
                element.classList.add('score-reveal'); // Thêm class để tạo hiệu ứng "reveal"
                setTimeout(() => {
                    element.classList.remove('score-reveal'); // Xóa class sau 600ms
                }, 600);
            }
        }
        
        requestAnimationFrame(animate); // Bắt đầu animation bằng cách gọi frame đầu tiên
    }

    // ================ HÀM CẬP NHẬT MỨC ĐỘ ĐÁNH GIÁ ================
    // Hàm cập nhật mức độ đánh giá dựa trên tổng điểm
    // Mục đích: Hiển thị đánh giá tổng quan về chất lượng CV (Excellent/Good/Average/Needs Improvement)
    function updateScoreLevel(totalScore) {
        const scorePercentage = (totalScore / 60) * 100; // Tính phần trăm điểm (tổng tối đa là 60 điểm)
        const levelElement = document.querySelector('#score-total').closest('.score-item').querySelector('.text-sm'); // Tìm element hiển thị mức độ trong score-item chứa tổng điểm
        
        if (levelElement) {
            let level = ''; // Text mức độ (Excellent, Good, Average, Needs Improvement)
            let levelClass = ''; // CSS class cho styling màu sắc khác nhau
            
            // Phân loại mức độ dựa trên phần trăm điểm với các ngưỡng cụ thể
            if (scorePercentage >= 80) {
                level = 'Excellent'; // Xuất sắc (80-100%)
                levelClass = 'score-level-excellent'; // Class CSS cho màu xanh lá
            } else if (scorePercentage >= 60) {
                level = 'Good'; // Tốt (60-79%)
                levelClass = 'score-level-good'; // Class CSS cho màu xanh dương
            } else if (scorePercentage >= 40) {
                level = 'Average'; // Trung bình (40-59%)
                levelClass = 'score-level-average'; // Class CSS cho màu vàng
            } else {
                level = 'Needs Improvement'; // Cần cải thiện (0-39%)
                levelClass = 'score-level-needs-improvement'; // Class CSS cho màu đỏ
            }
            
            // Cập nhật nội dung text và style CSS cho element hiển thị mức độ
            levelElement.textContent = level; // Cập nhật text
            levelElement.className = `text-sm text-gray-500 mt-1 ${levelClass}`; // Cập nhật CSS class
        }
    };



    // ================ XỬ LÝ FORM GỬI GỢI Ý ================
    // Xử lý form gửi gợi ý chỉnh sửa CV
    // LƯU Ý: Đã xóa đoạn submit truyền thống để tránh submit form lại
    // document.getElementById('editSuggestionsForm')?.addEventListener('submit', function(e) {
    //     e.preventDefault();
    //     document.getElementById('loadingModal').classList.remove('hidden');
    //     // Simulate form submission with animation
    //     setTimeout(() => {
    //         document.getElementById('loadingModal').classList.add('hidden');
    //         this.submit();
    //     }, 1500);
    // });

    // ================ MODAL LOADING ================
    // Quản lý modal loading khi gửi request
    // Mục đích: Hiển thị modal loading toàn màn hình khi đang xử lý các request tốn thời gian
    const loadingModal = {
        element: document.getElementById('loadingModal'), // Element modal chính
        modalContent: document.querySelector('#loadingModal > div'), // Nội dung bên trong modal
        controller: null, // AbortController để hủy request đang chạy

        // Hiển thị modal loading với animation mượt mà
        show: function () {
            if (!this.element) {
                console.error('Loading modal element not found!'); // Log lỗi nếu không tìm thấy element
                return;
            }

            // Hiển thị modal với animation fade in
            this.element.classList.remove('hidden'); // Xóa class ẩn
            this.element.style.display = 'flex'; // Hiển thị với flexbox để căn giữa
            this.element.classList.remove('show'); // Xóa class show để reset animation
            if (this.modalContent) this.modalContent.classList.remove('show'); // Reset animation cho content

            // Thêm class show sau 50ms để tạo hiệu ứng fade in mượt mà
            setTimeout(() => {
                this.element.classList.add('show'); // Thêm class show cho modal
                if (this.modalContent) this.modalContent.classList.add('show'); // Thêm class show cho content
            }, 50);
        },

        // Ẩn modal loading với animation fade out
        hide: function () {
            if (!this.element) return; // Thoát nếu không có element

            // Hủy request đang chạy nếu có để tránh memory leak
            if (this.controller) {
                this.controller.abort(); // Hủy request
            }

            // Ẩn modal với animation fade out
            this.element.classList.remove('show'); // Xóa class show để bắt đầu fade out
            if (this.modalContent) this.modalContent.classList.remove('show'); // Xóa class show cho content

            // Ẩn hoàn toàn sau 300ms (thời gian animation fade out)
            setTimeout(() => {
                this.element.style.display = 'none'; // Ẩn hoàn toàn
                this.element.classList.add('hidden'); // Thêm class hidden
            }, 300);
        },

        // Khởi tạo modal loading và gắn event listener cho nút cancel
        init: function () {
            const cancelBtn = document.getElementById('cancelAnalyzeBtn'); // Tìm nút cancel
            if (cancelBtn) {
                cancelBtn.addEventListener('click', () => {
                    // Hủy request khi ấn cancel để dừng xử lý
                    if (this.controller) this.controller.abort(); // Hủy request đang chạy
                    this.hide(); // Ẩn modal loading
                });
            }
        }
    };

    // Ẩn modal khi trang load (phòng trường hợp modal bị mở từ trước)
    // Đảm bảo modal không hiển thị khi trang được load lại
    loadingModal.hide();
    
    // Ẩn modal khi navigate back/forward (bfcache - back-forward cache)
    // Xử lý trường hợp người dùng sử dụng nút back/forward của trình duyệt
    window.addEventListener('pageshow', function() {
        loadingModal.hide(); // Ẩn modal để tránh hiển thị không mong muốn
    });

    // ================ SIDEBAR CỐ ĐỊNH ================
    // Quản lý sidebar cố định khi cuộn trang
    // Mục đích: Giữ sidebar luôn hiển thị khi người dùng cuộn trang để dễ dàng truy cập các chức năng
    const stickySidebar = {
        element: document.querySelector('.sticky-sidebar'), // Element sidebar cần cố định
        mainContent: document.querySelector('.lg\\:col-span-2'), // Nội dung chính của trang
        // Đã xóa: footer: document.querySelector('footer'),

        // Cập nhật vị trí và kích thước của sidebar
        updatePosition: function () {
            if (!this.element) return; // Thoát nếu không tìm thấy sidebar

            // Trên mobile (< 1024px), không cố định sidebar để tránh che nội dung
            if (window.innerWidth < 1024) {
                this.element.style.position = 'static'; // Vị trí tĩnh
                this.element.style.height = 'auto'; // Chiều cao tự động
                return;
            }

            // Tính toán vị trí và kích thước cho sidebar trên desktop
            const sidebarRect = this.element.getBoundingClientRect(); // Lấy thông tin vị trí hiện tại
            const maxHeight = window.innerHeight - 40; // Chiều cao tối đa = viewport - 40px (để có khoảng đệm)
            this.element.style.maxHeight = `${maxHeight}px`; // Giới hạn chiều cao tối đa

            this.element.style.top = '20px'; // Cách đỉnh màn hình 20px để tạo khoảng đệm
        },

        // Khởi tạo sticky sidebar và gắn các event listener
        init: function () {
            if (!this.element) return; // Thoát nếu không tìm thấy sidebar

            this.updatePosition(); // Cập nhật vị trí ban đầu khi trang load
            const debouncedUpdate = debounce(() => this.updatePosition(), 100); // Debounce để tối ưu performance (chỉ gọi sau 100ms)
            window.addEventListener('resize', debouncedUpdate); // Cập nhật khi thay đổi kích thước cửa sổ
            window.addEventListener('scroll', debouncedUpdate); // Cập nhật khi cuộn trang
        }
    };

    // ================ XỬ LÝ FORM ================
    // Quản lý việc gửi form gợi ý chỉnh sửa CV
    // Mục đích: Xử lý việc gửi form gợi ý chỉnh sửa CV với loading state và error handling
    const formHandler = {
        form: null, // Form element cần xử lý
        controller: null, // AbortController để hủy request khi cần
        submitting: false, // Trạng thái đang gửi form (tránh submit nhiều lần)
        aborted: false, // Trạng thái đã hủy request

        // Gửi form với async/await để xử lý bất đồng bộ
        submit: async function (e) {
            e.preventDefault(); // Ngăn form submit mặc định của trình duyệt
            if (this.submitting) return; // Nếu đang gửi thì không gửi nữa (tránh duplicate submit)
            this.submitting = true; // Đánh dấu đang gửi
            this.aborted = false; // Reset trạng thái hủy
            loadingModal.show(); // Hiển thị modal loading toàn màn hình
            this.controller = new AbortController(); // Tạo controller mới để có thể hủy request
            loadingModal.controller = this.controller; // Gán controller cho modal để có thể hủy từ modal

            try {
                // Gửi request lên server với FormData
                const response = await fetch(this.form.action, {
                    method: 'POST', // Phương thức POST để gửi dữ liệu
                    body: new FormData(this.form), // Dữ liệu form được đóng gói thành FormData
                    signal: this.controller.signal, // Signal để có thể hủy request khi cần
                    headers: {
                        'X-RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value // Token bảo mật chống CSRF
                    }
                });

                if (this.aborted) return; // Nếu đã abort, không xử lý/log gì nữa (tránh lỗi)

                // Xử lý response từ server theo các trường hợp khác nhau
                if (response.redirected) {
                    window.location.href = response.url; // Redirect nếu server yêu cầu chuyển hướng
                } else if (response.ok) {
                    window.location.reload(); // Reload trang nếu thành công để hiển thị kết quả mới
                } else {
                    throw new Error('Network response was not ok'); // Ném lỗi nếu response không thành công
                }
            } catch (error) {
                // Xử lý các loại lỗi khác nhau
                if (error.name === 'AbortError') {
                    // Lỗi do request bị hủy (người dùng ấn cancel)
                    this.aborted = true;
                    toast.show('Request has been cancelled.', 'warning'); // Thông báo đã hủy
                    // Không reload, không xử lý gì thêm
                    return;
                } else {
                    // Lỗi khác (mạng, server, v.v.)
                    toast.show('An error occurred while sending the request. Please try again.', 'error'); // Thông báo lỗi
                }
            } finally {
                // Luôn chạy sau khi try/catch hoàn thành
                loadingModal.hide(); // Ẩn modal loading
                this.submitting = false; // Reset trạng thái gửi để có thể submit lại
            }
        },

        // Hủy request đang gửi khi người dùng muốn dừng
        abort: function () {
            this.aborted = true; // Đánh dấu đã hủy
            if (this.controller) {
                this.controller.abort(); // Hủy request thực tế
            }
        },

        // Khởi tạo form handler và gắn các event listener
        init: function () {
            this.form = document.getElementById('editSuggestionsForm'); // Tìm form theo ID
            if (this.form) {
                this.form.addEventListener('submit', (e) => this.submit(e)); // Gắn event listener cho submit
            } else {
                console.error('Form #editSuggestionsForm not found!'); // Log lỗi nếu không tìm thấy form
            }
            
            // Logic cho nút Cancel trong edit suggestions
            // Cho phép người dùng hủy việc gửi form khi đang xử lý
            const cancelBtn = document.getElementById('cancelAnalyzeBtn');
            if (cancelBtn) {
                cancelBtn.addEventListener('click', (evt) => {
                    evt.preventDefault(); // Ngăn hành vi mặc định của button
                    this.abort(); // Hủy request đang chạy
                });
            }
        }
    };
  
    // ================ GENERATE CV FORM HANDLING ================
//     const generateCVFormHandler = {
//         form: null,
//         controller: null,
//         submitting: false,
//         aborted: false,
//         loadingModal: null,

//         submit: async function (e) {
//             e.preventDefault();
//             if (this.submitting) return;
//             this.submitting = true;
//             this.aborted = false;
//             this.showLoadingModal();
//             this.controller = new AbortController();

//             try {
//                 const response = await fetch(this.form.action, {
//                     method: 'POST',
//                     body: new FormData(this.form),
//                     signal: this.controller.signal,
//                     headers: {
//                         'X-RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
//                     }
//                 });

//                 if (this.aborted) return; // Nếu đã abort, không xử lý/log gì nữa

//                 if (response.redirected) {
//                     window.location.href = response.url;
//                 } else if (response.ok) {
//                     window.location.reload();
//                 } else {
//                     throw new Error('Network response was not ok');
//                 }
//             } catch (error) {
//                 if (error.name === 'AbortError') {
//                     this.aborted = true;
//                     toast.show('CV generation has been cancelled.', 'warning');
//                     return;
//                 } else {
//                     toast.show('An error occurred while generating your CV. Please try again.', 'error');
//                 }
//             } finally {
//                 this.hideLoadingModal();
//                 this.submitting = false;
//             }
//         },

//         abort: function () {
//             this.aborted = true;
//             if (this.controller) {
//                 this.controller.abort();
//             }
//         },

//         showLoadingModal: function () {
//             if (this.loadingModal) {
//                 this.loadingModal.classList.remove('hidden');
//                 this.loadingModal.style.display = 'flex';
//             }
//         },

//         hideLoadingModal: function () {
//             if (this.loadingModal) {
//                 this.loadingModal.classList.add('hidden');
//                 this.loadingModal.style.display = 'none';
//             }
//         },

//         init: function () {
//             this.form = document.getElementById('generateFinalCVForm');
//             this.loadingModal = document.getElementById('generateCVLoadingModal');
            
//             if (this.form) {
//                 this.form.addEventListener('submit', (e) => this.submit(e));
//             } else {
//                 console.error('Form #generateFinalCVForm not found!');
//             }

//             // Cancel button logic for generate CV
//             const cancelBtn = document.getElementById('cancelGenerateCVBtn');
//             if (cancelBtn) {
//                 cancelBtn.addEventListener('click', (evt) => {
//                     evt.preventDefault();
//                     this.abort();
//                     this.hideLoadingModal();
//                 });
//             }
//         }
//     };
          // ================ THÔNG BÁO TOAST ================
    // Hệ thống thông báo popup nhỏ ở góc màn hình
    // Mục đích: Hiển thị thông báo ngắn gọn cho người dùng về các hành động thành công/lỗi
    const toast = {
        // Hiển thị thông báo toast với animation
        show: function (message, type = 'info') {
            const toastElement = document.createElement('div'); // Tạo element toast mới
            toastElement.className = `toast fixed bottom-4 right-4 px-4 py-3 rounded-lg shadow-lg text-white font-medium flex items-center ${this.getColor(type)}`; // CSS classes với vị trí cố định
            toastElement.innerHTML = `<i class="${this.getIcon(type)} mr-2"></i>${message}`; // Nội dung với icon tương ứng

            document.body.appendChild(toastElement); // Thêm toast vào DOM
            setTimeout(() => toastElement.classList.add('fade-in'), 10); // Thêm animation fade in sau 10ms

            // Tự động ẩn sau 3 giây với animation fade out
            setTimeout(() => {
                toastElement.classList.remove('fade-in'); // Bắt đầu fade out
                setTimeout(() => toastElement.remove(), 300); // Xóa element sau khi fade out xong (300ms)
            }, 3000);
        },

        // Lấy màu sắc CSS class cho từng loại thông báo
        getColor: function (type) {
            const colors = {
                'success': 'bg-green-500', // Màu xanh lá cho thành công
                'error': 'bg-red-500', // Màu đỏ cho lỗi
                'warning': 'bg-yellow-500', // Màu vàng cho cảnh báo
                'info': 'bg-blue-500' // Màu xanh dương cho thông tin
            };
            return colors[type] || 'bg-blue-500'; // Mặc định màu xanh dương nếu không tìm thấy type
        },

        // Lấy icon FontAwesome cho từng loại thông báo
        getIcon: function (type) {
            const icons = {
                'success': 'fas fa-check-circle', // Icon check tròn cho thành công
                'error': 'fas fa-exclamation-circle', // Icon cảnh báo tròn cho lỗi
                'warning': 'fas fa-exclamation-triangle', // Icon tam giác cảnh báo cho warning
                'info': 'fas fa-info-circle' // Icon thông tin tròn cho info
            };
            return icons[type] || 'fas fa-info-circle'; // Mặc định icon thông tin nếu không tìm thấy type
        }
    };

    // ================ HÀM TIỆN ÍCH ================
    // Hàm debounce để tối ưu performance khi gọi function nhiều lần
    // Mục đích: Tránh gọi function quá nhiều lần trong thời gian ngắn (ví dụ: scroll, resize)
    function debounce(func, wait) {
        let timeout; // Biến lưu timeout ID
        return (...args) => {
            clearTimeout(timeout); // Xóa timeout cũ nếu có
            timeout = setTimeout(() => func.apply(this, args), wait); // Tạo timeout mới và chỉ gọi function sau khi đợi đủ thời gian
        };
    }

    // ================ KHỞI TẠO ================
    // Khởi tạo tất cả các module khi trang load
    // Đảm bảo tất cả các chức năng được thiết lập đúng cách khi DOM ready
    loadingModal.init(); // Khởi tạo modal loading và gắn event listener
    stickySidebar.init(); // Khởi tạo sidebar cố định và gắn scroll/resize listener
    formHandler.init(); // Khởi tạo xử lý form và gắn submit listener
    generateCVFormHandler.init();

    // ================ TƯƠNG TÁC CHẤM ĐIỂM CV NÂNG CAO ================
    // Thêm hiệu ứng hover cho các item điểm để tăng trải nghiệm người dùng
    // Mục đích: Tạo phản hồi trực quan khi người dùng di chuột qua các score item
    document.querySelectorAll('.score-item').forEach(item => {
        // Khi di chuột vào - nâng item lên và thêm shadow
        item.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-2px)'; // Nâng lên 2px khi hover
            this.style.boxShadow = '0 8px 25px rgba(0, 0, 0, 0.1)'; // Thêm shadow đậm hơn
        });
        
        // Khi di chuột ra - trở về vị trí ban đầu
        item.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)'; // Trở về vị trí ban đầu
            this.style.boxShadow = '0 1px 3px rgba(0, 0, 0, 0.05)'; // Shadow nhẹ như ban đầu
        });
    });

    // Thêm hiệu ứng click cho các item điểm
    document.querySelectorAll('.score-item').forEach(item => {
        item.addEventListener('click', function() {
            this.style.transform = 'scale(0.98)'; // Thu nhỏ 98% khi click
            setTimeout(() => {
                this.style.transform = 'scale(1)'; // Trở về kích thước ban đầu
            }, 150); // Sau 150ms
        });
    });

    // ================ TƯƠNG TÁC NÚT NÂNG CAO ================
    // Thêm hiệu ứng tương tác cho nút chấm điểm CV
    const btnScoreCV = document.getElementById('btnScoreCV');
    if (btnScoreCV) {
        // Hiệu ứng hover - nâng lên và phóng to
        btnScoreCV.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-3px) scale(1.05)'; // Nâng lên 3px và phóng to 105%
            this.style.boxShadow = '0 15px 35px rgba(59, 130, 246, 0.4)'; // Thêm shadow xanh
        });
        
        // Hiệu ứng khi rời chuột - trở về trạng thái ban đầu
        btnScoreCV.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0) scale(1)'; // Trở về vị trí và kích thước ban đầu
            this.style.boxShadow = ''; // Xóa shadow
        });
        
        // Hiệu ứng khi nhấn chuột - thu nhỏ nhẹ
        btnScoreCV.addEventListener('mousedown', function() {
            this.style.transform = 'translateY(-1px) scale(0.98)'; // Thu nhỏ 98% và nâng lên 1px
        });
        
        // Hiệu ứng khi thả chuột - trở về trạng thái hover
        btnScoreCV.addEventListener('mouseup', function() {
            this.style.transform = 'translateY(-3px) scale(1.05)'; // Trở về trạng thái hover
        });

        // Thêm hiệu ứng ripple (sóng lan tỏa) khi click
        btnScoreCV.addEventListener('click', function(e) {
            const ripple = document.createElement('span'); // Tạo element ripple
            const rect = this.getBoundingClientRect(); // Lấy kích thước và vị trí button
            const size = Math.max(rect.width, rect.height); // Kích thước ripple = cạnh lớn nhất
            const x = e.clientX - rect.left - size / 2; // Vị trí X của ripple
            const y = e.clientY - rect.top - size / 2; // Vị trí Y của ripple
            
            // Thiết lập style cho ripple
            ripple.style.width = ripple.style.height = size + 'px';
            ripple.style.left = x + 'px';
            ripple.style.top = y + 'px';
            ripple.classList.add('ripple'); // Thêm class CSS
            
            this.appendChild(ripple); // Thêm ripple vào button
            
            // Xóa ripple sau 600ms
            setTimeout(() => {
                ripple.remove();
            }, 600);
        });
    }

    // Enhanced score item interactions
    document.querySelectorAll('.score-item').forEach((item, index) => {
        // Add staggered entrance animation
        item.style.animationDelay = `${index * 0.1}s`;
        
        // Add click effect
        item.addEventListener('click', function() {
            this.style.transform = 'scale(0.95)';
            setTimeout(() => {
                this.style.transform = '';
            }, 150);
        });

        // Add hover sound effect (optional)
        item.addEventListener('mouseenter', function() {
            // Add subtle glow effect
            this.style.boxShadow = '0 8px 25px rgba(0, 0, 0, 0.15)';
        });

        item.addEventListener('mouseleave', function() {
            this.style.boxShadow = '';
        });
    });

    // Thêm animation floating cho các element header
    const headerElements = document.querySelectorAll('#cv-score-block .bg-gradient-to-br');
    headerElements.forEach(element => {
        element.style.animation = 'float 3s ease-in-out infinite'; // Animation floating 3s lặp vô hạn
    });

    // Animation fill progress bar
    function animateProgressBars() {
        const progressBars = document.querySelectorAll('.score-item .bg-gradient-to-r'); // Tìm tất cả progress bar
        progressBars.forEach((bar, index) => {
            setTimeout(() => {
                const width = bar.style.width; // Lưu width hiện tại
                bar.style.width = '0%'; // Reset về 0%
                setTimeout(() => {
                    bar.style.width = width; // Khôi phục width ban đầu
                }, 100); // Sau 100ms
            }, index * 200); // Delay tăng dần 200ms cho mỗi bar
        });
    }

    // Kích hoạt animation progress bar khi score table hiển thị
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.type === 'attributes' && mutation.attributeName === 'style') { // Khi style thay đổi
                const scoreTable = document.getElementById('cv-score-table'); // Tìm score table
                if (scoreTable && scoreTable.style.display !== 'none') { // Nếu hiển thị (không phải none)
                    setTimeout(animateProgressBars, 500); // Chạy animation sau 500ms
                }
            }
        });
    });

    const scoreTable = document.getElementById('cv-score-table');
    if (scoreTable) {
        observer.observe(scoreTable, { attributes: true }); // Quan sát thay đổi attributes
    }

    // Animation thành công cho điểm cao (>= 80%)
    function addHighScoreAnimation() {
        const scoreItems = document.querySelectorAll('.score-item'); // Tìm tất cả score items
        scoreItems.forEach(item => {
            const scoreText = item.querySelector('.font-bold'); // Tìm text hiển thị điểm
            if (scoreText) {
                const score = parseInt(scoreText.textContent.split('/')[0]); // Lấy điểm hiện tại
                const maxScore = parseInt(scoreText.textContent.split('/')[1]); // Lấy điểm tối đa
                const percentage = (score / maxScore) * 100; // Tính phần trăm
                
                if (percentage >= 80) { // Nếu >= 80%
                    item.classList.add('high-score'); // Thêm class high-score
                    setTimeout(() => {
                        item.classList.remove('high-score'); // Xóa class sau 2s
                    }, 2000);
                }
            }
        });
    }

    // Cập nhật điểm với animation nâng cao
    function updateScoreWithEnhancedAnimation(elementId, finalScore, colorClass) {
        const element = document.getElementById(elementId); // Tìm element theo ID
        if (!element) return; // Thoát nếu không tìm thấy
        
        const progressBar = element.closest('.score-item').querySelector('.bg-gradient-to-r'); // Tìm progress bar
        const startScore = 0; // Điểm bắt đầu
        const duration = 2000; // Thời gian animation (2s)
        const startTime = performance.now(); // Thời gian bắt đầu
        
        // Tạo audio context cho hiệu ứng âm thanh (tùy chọn)
        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        
        function animate(currentTime) {
            const elapsed = currentTime - startTime; // Thời gian đã trôi qua
            const progress = Math.min(elapsed / duration, 1); // Tiến độ (0-1)
            
            // Hàm easing nâng cao (easeOutBack)
            const easeOutBack = 1 + 2.70158 * Math.pow(progress - 1, 3) + 1.70158 * Math.pow(progress - 1, 2);
            const currentScore = Math.floor(easeOutBack * (finalScore - startScore) + startScore); // Tính điểm hiện tại
            
            // Cập nhật text điểm với hiệu ứng bounce
            element.textContent = `${currentScore} / ${elementId === 'score-total' ? '60' : '10'}`; // Cập nhật text
            element.style.transform = 'scale(1.1)'; // Phóng to 110%
            setTimeout(() => {
                element.style.transform = 'scale(1)'; // Trở về kích thước ban đầu
            }, 100); // Sau 100ms
            
            // Cập nhật progress bar với animation nâng cao
            if (progressBar) {
                const percentage = (currentScore / (elementId === 'score-total' ? 60 : 10)) * 100; // Tính phần trăm
                progressBar.style.width = `${percentage}%`; // Cập nhật width
                
                // Thêm hiệu ứng glow trong quá trình animation
                progressBar.style.boxShadow = `0 0 10px ${getColorForClass(colorClass)}`;
            }
            
            // Phát hiệu ứng âm thanh nhẹ
            if (currentScore % 2 === 0 && currentScore > 0) { // Chỉ phát khi điểm chẵn và > 0
                playTickSound(audioContext, currentScore / finalScore); // Phát âm thanh với volume tương ứng
            }
            
            if (progress < 1) {
                requestAnimationFrame(animate);
            } else {
                // Completion effects
                element.classList.add('score-reveal');
                setTimeout(() => {
                    element.classList.remove('score-reveal');
                    if (progressBar) {
                        progressBar.style.boxShadow = '';
                    }
                }, 600);
                
                // Add high score animation if applicable
                addHighScoreAnimation();
            }
        }
        
        requestAnimationFrame(animate);
    }

    // Hàm helper để lấy màu cho hiệu ứng glow
    function getColorForClass(colorClass) {
        const colors = {
            'blue': 'rgba(59, 130, 246, 0.6)', // Màu xanh dương với độ trong suốt
            'green': 'rgba(16, 185, 129, 0.6)', // Màu xanh lá với độ trong suốt
            'purple': 'rgba(139, 92, 246, 0.6)', // Màu tím với độ trong suốt
            'orange': 'rgba(249, 115, 22, 0.6)', // Màu cam với độ trong suốt
            'pink': 'rgba(236, 72, 153, 0.6)', // Màu hồng với độ trong suốt
            'teal': 'rgba(20, 184, 166, 0.6)', // Màu teal với độ trong suốt
            'indigo': 'rgba(99, 102, 241, 0.6)' // Màu indigo với độ trong suốt
        };
        return colors[colorClass] || 'rgba(59, 130, 246, 0.6)'; // Trả về màu xanh dương mặc định
    }

    // Thêm hiệu ứng particle cho button click
    function createParticleEffect(x, y, color) {
        const particles = 8; // Số lượng particle
        for (let i = 0; i < particles; i++) {
            const particle = document.createElement('div'); // Tạo element particle
            particle.style.position = 'absolute'; // Vị trí tuyệt đối
            particle.style.left = x + 'px'; // Vị trí X
            particle.style.top = y + 'px'; // Vị trí Y
            particle.style.width = '4px'; // Chiều rộng 4px
            particle.style.height = '4px'; // Chiều cao 4px
            particle.style.backgroundColor = color; // Màu nền
            particle.style.borderRadius = '50%'; // Hình tròn
            particle.style.pointerEvents = 'none'; // Không nhận sự kiện chuột
            particle.style.zIndex = '1000'; // Layer cao
            
            document.body.appendChild(particle); // Thêm vào body
            
            const angle = (i / particles) * 2 * Math.PI; // Góc phân bố đều
            const velocity = 50 + Math.random() * 50; // Vận tốc ngẫu nhiên 50-100
            const vx = Math.cos(angle) * velocity; // Vận tốc theo trục X
            const vy = Math.sin(angle) * velocity; // Vận tốc theo trục Y
            
            let opacity = 1; // Độ trong suốt ban đầu
            const animate = () => {
                const currentX = parseFloat(particle.style.left); // Vị trí X hiện tại
                const currentY = parseFloat(particle.style.top); // Vị trí Y hiện tại
                
                particle.style.left = (currentX + vx * 0.1) + 'px'; // Cập nhật vị trí X
                particle.style.top = (currentY + vy * 0.1) + 'px'; // Cập nhật vị trí Y
                particle.style.opacity = opacity; // Cập nhật độ trong suốt
                
                opacity -= 0.02; // Giảm độ trong suốt
                
                if (opacity > 0) { // Nếu còn trong suốt
                    requestAnimationFrame(animate); // Tiếp tục animation
                } else {
                    particle.remove(); // Xóa particle
                }
            };
            
            requestAnimationFrame(animate); // Bắt đầu animation
        }
    }

    // Nút click nâng cao với hiệu ứng particle
    if (btnScoreCV) {
        btnScoreCV.addEventListener('click', function(e) {
            const rect = this.getBoundingClientRect(); // Lấy kích thước và vị trí button
            const x = e.clientX - rect.left; // Vị trí X tương đối trong button
            const y = e.clientY - rect.top; // Vị trí Y tương đối trong button
            createParticleEffect(x, y, '#3b82f6'); // Tạo hiệu ứng particle màu xanh
        });
    }



    // ================ XỬ LÝ GỢI Ý CÔNG VIỆC ================
    // Quản lý việc gợi ý công việc phù hợp dựa trên CV
    // Mục đích: Sử dụng AI để phân tích kỹ năng và kinh nghiệm, sau đó gợi ý công việc phù hợp
    jobRecommendHandler = {
        // Các element cần thiết cho job recommendation
        // Lưu trữ tất cả các element DOM cần thiết để tránh query nhiều lần
        elements: {
            btnJobRecommend: document.getElementById('btnJobRecommend'), // Nút gợi ý công việc (trạng thái ban đầu)
            btnRetryJobRecommend: document.getElementById('btnRetryJobRecommend'), // Nút thử lại (khi có lỗi)
            jobInitialState: document.getElementById('jobInitialState'), // Trạng thái ban đầu (hiển thị nút "Get Recommendations")
            jobLoadingState: document.getElementById('jobLoadingState'), // Trạng thái loading (spinner + text)
            jobRecommendResult: document.getElementById('jobRecommendResult'), // Kết quả gợi ý từ API
            jobRecommendResultFromSession: document.getElementById('jobRecommendResultFromSession'), // Kết quả từ session (khi reload trang)
            jobErrorState: document.getElementById('jobErrorState') // Trạng thái lỗi (thông báo lỗi)
        },
        abortController: null, // AbortController để hủy request khi cần

        // Hiển thị trạng thái cụ thể và ẩn các trạng thái khác
        // Mục đích: Quản lý việc chuyển đổi giữa các trạng thái UI (initial, loading, result, error)
        showState: function(stateName) {
            // Ẩn tất cả các trạng thái trước khi hiển thị trạng thái mới
            Object.values(this.elements).forEach(element => {
                if (element && element.style) {
                    element.style.display = 'none'; // Ẩn tất cả element
                }
            });
            
            // Hiển thị trạng thái được yêu cầu
            if (this.elements[stateName]) {
                this.elements[stateName].style.display = 'block'; // Hiển thị element tương ứng
            }
        },

        // Bắt đầu loading - tạo controller mới và hiển thị trạng thái loading
        // Mục đích: Chuẩn bị cho việc gửi request và hiển thị loading state
        startLoading: function() {
            this.abortController = new AbortController(); // Tạo controller mới để có thể hủy request
            this.showState('jobLoadingState'); // Hiển thị trạng thái loading (spinner + text)
        },

        // Hiển thị lỗi - hiển thị trạng thái lỗi và log lỗi
        // Mục đích: Xử lý và hiển thị lỗi khi có vấn đề với request
        showError: function(message) {
            this.showState('jobErrorState'); // Hiển thị trạng thái lỗi cho người dùng
            console.error('Job recommendation error:', message); // Log lỗi để debug
        },

        // Hiển thị kết quả gợi ý công việc từ API
        // Mục đích: Render HTML cho kết quả gợi ý công việc với giao diện đẹp và thông tin chi tiết
        showResults: function(data) {
            this.showState('jobRecommendResult'); // Chuyển sang trạng thái hiển thị kết quả
            
            let html = ''; // Biến lưu HTML kết quả
            
            // Xử lý trường hợp có công việc được gợi ý (thành công)
            if (data && data.recommendedJob) {
                html = `
                    <div class="space-y-6">
                        <!-- Header với icon thành công -->
                        <div class="text-center mb-6">
                            <div class="w-16 h-16 mx-auto mb-4 bg-gradient-to-br from-green-400 to-green-600 rounded-full flex items-center justify-center">
                                <i class="fas fa-check text-white text-2xl"></i>
                            </div>
                            <h3 class="text-xl font-bold text-gray-800 mb-2">Perfect Match Found!</h3>
                            <p class="text-gray-600">Based on your skills and experience, we recommend:</p>
                        </div>

                        <!-- Job Card - Hiển thị công việc được gợi ý -->
                        <div class="bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-200 rounded-xl p-6 hover:shadow-lg transition-all duration-300">
                            <div class="flex items-start mb-4">
                                <div class="flex items-center">
                                    <div class="w-12 h-12 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-lg flex items-center justify-center mr-4">
                                        <i class="fas fa-briefcase text-white text-lg"></i>
                                    </div>
                                    <div>
                                        <h4 class="text-xl font-bold text-gray-900 mb-1">${data.recommendedJob}</h4>
                                        <p class="text-sm text-gray-600">Recommended for you</p>
                                    </div>
                                </div>
                            </div>

                            <!-- Skills to Improve - Hiển thị kỹ năng cần cải thiện nếu có -->
                            ${data.missingSkills && data.missingSkills.length > 0 ? `
                                <div class="bg-gradient-to-br from-yellow-50 to-orange-50 border border-yellow-200 rounded-xl p-4">
                                    <div class="flex items-center mb-3">
                                        <div class="w-8 h-8 bg-gradient-to-br from-yellow-500 to-orange-500 rounded-lg flex items-center justify-center mr-3">
                                            <i class="fas fa-lightbulb text-white text-sm"></i>
                                        </div>
                                        <h5 class="text-sm font-semibold text-yellow-800">Skills to Improve</h5>
                                    </div>
                                    <div class="flex flex-wrap gap-2">
                                        ${data.missingSkills.map(skill => `
                                            <span class="bg-gradient-to-r from-yellow-100 to-orange-100 text-yellow-800 px-3 py-1 rounded-full text-xs font-medium border border-yellow-200 hover:from-yellow-200 hover:to-orange-200 transition-all duration-200">
                                                ${skill}
                                            </span>
                                        `).join('')}
                                    </div>
                                </div>
                            ` : ''}
                        </div>

                        <!-- Action Buttons - Nút hành động -->
                        <div class="flex flex-col sm:flex-row gap-2">
                            <a href="/CVAnalysis/JobPrediction" class="group flex-1 flex justify-center items-center px-4 py-2 border border-gray-300 rounded-lg shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-gray-300 transition-all duration-300 transform hover:-translate-y-0.5 hover:shadow-md">
                                <i class="fas fa-search mr-2 group-hover:animate-bounce"></i>View More Jobs
                            </a>
                        </div>
                    </div>
                `;
            } 
            // Xử lý trường hợp cần cải thiện (AI phát hiện cần bổ sung thêm thông tin)
            else if (data && data.improvementPlan) {
                html = `
                    <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-6">
                        <div class="flex items-start">
                            <i class="fas fa-exclamation-triangle text-yellow-500 mt-1 mr-3"></i>
                            <div>
                                <h4 class="text-sm font-semibold text-yellow-800 mb-2">Improvement Needed</h4>
                                <p class="text-sm text-yellow-700">${data.improvementPlan}</p>
                            </div>
                        </div>
                    </div>
                `;
            } 
            // Xử lý trường hợp không tìm thấy gợi ý (AI không thể tìm thấy công việc phù hợp)
            else {
                html = `
                    <div class="text-center py-8">
                        <div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
                            <i class="fas fa-info-circle text-gray-400 text-xl"></i>
                        </div>
                        <h4 class="text-lg font-semibold text-gray-800 mb-2">No Recommendations Found</h4>
                        <p class="text-sm text-gray-600">We couldn't find suitable job matches for your current profile.</p>
                    </div>
                `;
            }
            
            this.elements.jobRecommendResult.innerHTML = html; // Cập nhật nội dung HTML cho element kết quả
        },

        // Hiển thị kết quả từ session (khi trang load với dữ liệu có sẵn)
        // Mục đích: Hiển thị lại kết quả gợi ý công việc khi người dùng reload trang hoặc quay lại
        showResultsFromSession: function(data) {
            if (this.elements.jobRecommendResultFromSession) {
                this.elements.jobRecommendResultFromSession.style.display = 'block'; // Hiển thị element
                
                let html = ''; // Biến lưu HTML kết quả
                
                // Xử lý trường hợp có công việc được gợi ý (giống như showResults nhưng cho session)
                if (data && data.recommendedJob) {
                    html = `
                        <div class="space-y-6">
                            <div class="text-center mb-6">
                                <div class="w-16 h-16 mx-auto mb-4 bg-gradient-to-br from-green-400 to-green-600 rounded-full flex items-center justify-center">
                                    <i class="fas fa-check text-white text-2xl"></i>
                                </div>
                                <h3 class="text-xl font-bold text-gray-800 mb-2">Perfect Match Found!</h3>
                                <p class="text-gray-600">Based on your skills and experience, we recommend:</p>
                            </div>

                            <!-- Job Card -->
                            <div class="bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-200 rounded-xl p-6 hover:shadow-lg transition-all duration-300">
                                <div class="flex items-start mb-4">
                                    <div class="flex items-center">
                                        <div class="w-12 h-12 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-lg flex items-center justify-center mr-4">
                                            <i class="fas fa-briefcase text-white text-lg"></i>
                                        </div>
                                        <div>
                                            <h4 class="text-xl font-bold text-gray-900 mb-1">${data.recommendedJob}</h4>
                                            <p class="text-sm text-gray-600">Recommended for you</p>
                                        </div>
                                    </div>
                                </div>

                                <!-- Skills to Improve -->
                                ${data.missingSkills && data.missingSkills.length > 0 ? `
                                    <div class="bg-gradient-to-br from-yellow-50 to-orange-50 border border-yellow-200 rounded-xl p-4">
                                        <div class="flex items-center mb-3">
                                            <div class="w-8 h-8 bg-gradient-to-br from-yellow-500 to-orange-500 rounded-lg flex items-center justify-center mr-3">
                                                <i class="fas fa-lightbulb text-white text-sm"></i>
                                            </div>
                                            <h5 class="text-sm font-semibold text-yellow-800">Skills to Improve</h5>
                                        </div>
                                        <div class="flex flex-wrap gap-2">
                                            ${data.missingSkills.map(skill => `
                                                <span class="bg-gradient-to-r from-yellow-100 to-orange-100 text-yellow-800 px-3 py-1 rounded-full text-xs font-medium border border-yellow-200 hover:from-yellow-200 hover:to-orange-200 transition-all duration-200">
                                                    ${skill}
                                                </span>
                                            `).join('')}
                                        </div>
                                    </div>
                                ` : ''}
                            </div>

                            <!-- Action Buttons -->
                            <div class="flex flex-col sm:flex-row gap-2">
                                <a href="/CVAnalysis/JobPrediction" class="group flex-1 flex justify-center items-center px-4 py-2 border border-gray-300 rounded-lg shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-gray-300 transition-all duration-300 transform hover:-translate-y-0.5 hover:shadow-md">
                                    <i class="fas fa-search mr-2 group-hover:animate-bounce"></i>View More Jobs
                                </a>
                            </div>
                        </div>
                    `;
                } 
                // Xử lý trường hợp cần cải thiện (AI phát hiện cần bổ sung thêm thông tin)
                else if (data && data.improvementPlan) {
                    html = `
                        <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-6">
                            <div class="flex items-start">
                                <i class="fas fa-exclamation-triangle text-yellow-500 mt-1 mr-3"></i>
                                <div>
                                    <h4 class="text-sm font-semibold text-yellow-800 mb-2">Improvement Needed</h4>
                                    <p class="text-sm text-yellow-700">${data.improvementPlan}</p>
                                </div>
                            </div>
                        </div>
                    `;
                } 
                // Xử lý trường hợp không tìm thấy gợi ý (AI không thể tìm thấy công việc phù hợp)
                else {
                    html = `
                        <div class="text-center py-8">
                            <div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
                                <i class="fas fa-info-circle text-gray-400 text-xl"></i>
                            </div>
                            <h4 class="text-lg font-semibold text-gray-800 mb-2">No Recommendations Found</h4>
                            <p class="text-sm text-gray-600">We couldn't find suitable job matches for your current profile.</p>
                        </div>
                    `;
                }
                
                this.elements.jobRecommendResultFromSession.innerHTML = html; // Cập nhật nội dung HTML cho element session
            }
        },

        // Xử lý click nút gợi ý công việc
        // Mục đích: Thu thập dữ liệu CV và gửi lên server để AI phân tích và gợi ý công việc
        handleClick: function() {
            // Lấy kỹ năng và kinh nghiệm làm việc từ các trường ẩn trong form
            // Các trường này được điền tự động từ kết quả phân tích CV trước đó
            const skills = document.getElementById('cvSkills').value || ''; // Kỹ năng được trích xuất
            const experience = document.getElementById('cvExperience').value || ''; // Kinh nghiệm làm việc
            const project = document.getElementById('cvProject').value || ''; // Dự án đã thực hiện
            
            // Kết hợp kinh nghiệm và dự án thành một chuỗi duy nhất
            // Để AI có thể phân tích toàn bộ thông tin làm việc
            let workExperience = '';
            if (experience && project) {
                workExperience = experience + '\n\n' + project; // Kết hợp cả hai với dòng trống ở giữa
            } else if (experience) {
                workExperience = experience; // Chỉ có kinh nghiệm làm việc
            } else if (project) {
                workExperience = project; // Chỉ có dự án (có thể là sinh viên mới tốt nghiệp)
            }
            
            this.startLoading(); // Bắt đầu loading state
            
            // Gửi request đến server để lấy gợi ý công việc từ AI
            const csrfToken = document.querySelector('input[name=__RequestVerificationToken]')?.value || '';
            
            fetch('/CVAnalysis/RecommendJobs', {
                method: 'POST', // Phương thức POST để gửi dữ liệu
                headers: {
                    'Content-Type': 'application/json', // Định dạng JSON
                    'RequestVerificationToken': csrfToken // Token bảo mật chống CSRF
                },
                body: JSON.stringify({ 
                    skills: skills, // Kỹ năng được trích xuất từ CV
                    workExperience: workExperience // Kinh nghiệm làm việc và dự án
                }),
                signal: this.abortController.signal // Signal để có thể hủy request khi cần
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json(); // Parse response thành JSON object
            })
            .then(data => {
                this.showResults(data); // Hiển thị kết quả gợi ý công việc
            })
            .catch((err) => {
                // Xử lý các loại lỗi khác nhau
                if (err.name === 'AbortError') {
                    this.showError('Request was cancelled'); // Thông báo đã hủy (người dùng ấn cancel)
                } else {
                    this.showError('Error fetching recommendations: ' + err.message); // Thông báo lỗi khác (mạng, server, v.v.)
                }
            });
        },

        // Khởi tạo job recommendation handler và gắn các event listener
        init: function() {
            // Gắn event listener cho nút gợi ý công việc (trạng thái ban đầu)
            if (this.elements.btnJobRecommend) {
                this.elements.btnJobRecommend.addEventListener('click', () => {
                    this.handleClick();
                });
            } else {
                // Try to find it again
                const btn = document.getElementById('btnJobRecommend');
                if (btn) {
                    btn.addEventListener('click', () => {
                        this.handleClick();
                    });
                }
            }
            
            // Gắn event listener cho nút thử lại (khi có lỗi)
            if (this.elements.btnRetryJobRecommend) {
                this.elements.btnRetryJobRecommend.addEventListener('click', () => this.handleClick());
            }

            // Kiểm tra xem có gợi ý công việc từ session không và hiển thị chúng
            // Để người dùng không mất kết quả khi reload trang
            const jobRecommendationsFromSession = document.getElementById('jobRecommendationsFromSession');
            if (jobRecommendationsFromSession && jobRecommendationsFromSession.value) {
                try {
                    const sessionData = JSON.parse(jobRecommendationsFromSession.value); // Parse JSON từ session
                    this.showResultsFromSession(sessionData); // Hiển thị kết quả từ session
                } catch (error) {
                    console.error('Error parsing job recommendations from session:', error); // Log lỗi nếu parse thất bại
                }
            }
        }
    };
    
    // Nút Cancel cho job recommend loading
    // Cho phép người dùng hủy việc gợi ý công việc khi đang xử lý
    const cancelBtn = document.getElementById('cancelAnalyzeBtn');
    if (cancelBtn) {
        cancelBtn.addEventListener('click', function () {
            if (jobRecommendHandler.abortController) jobRecommendHandler.abortController.abort(); // Hủy request đang chạy
            loadingModal.hide(); // Ẩn modal loading
            if (jobRecommendHandler.elements.btnJobRecommend) {
                jobRecommendHandler.elements.btnJobRecommend.disabled = false; // Enable lại nút gợi ý
                jobRecommendHandler.elements.btnJobRecommend.textContent = 'Get Smart Recommendations'; // Reset text về trạng thái ban đầu
            }
            if (loadingText) loadingText.textContent = 'The AI system is processing and generating edit suggestions. Please wait a moment...'; // Reset text loading
        });
    }

    // Nút quay lại trang Edit Suggestions
    // Kiểm tra xem người dùng đã sử dụng chức năng gợi ý chỉnh sửa CV chưa
    document.getElementById('btnBackEditSuggestions')?.addEventListener('click', function (e) {
        e.preventDefault(); // Ngăn hành vi mặc định của button
        fetch('/CVAnalysis/HasEditSuggestions') // Gọi API kiểm tra
            .then(response => response.json()) // Parse response
            .then(data => {
                if (data.hasSuggestions) {
                    window.location.href = '/CVAnalysis/EditSuggestions'; // Chuyển đến trang edit suggestions
                } else {
                    toast.show('You have not used the CV editing suggestion function', 'warning'); // Thông báo chưa sử dụng
                }
            })
            .catch(() => {
                toast.show('Unable to check edit suggestions', 'error'); // Thông báo lỗi khi kiểm tra
            });
    });

    // Modal chia sẻ kết quả - Hiển thị modal
    document.getElementById('shareResultBtn')?.addEventListener('click', function () {
        const shareModal = document.getElementById('shareModal');
        shareModal?.classList.remove('hidden'); // Hiển thị modal
        setTimeout(() => shareModal?.classList.add('show'), 10); // Thêm animation sau 10ms
    });

    // Modal chia sẻ kết quả - Ẩn modal
    document.getElementById('closeShareModal')?.addEventListener('click', function () {
        const shareModal = document.getElementById('shareModal');
        shareModal?.classList.remove('show'); // Bắt đầu ẩn
        setTimeout(() => shareModal?.classList.add('hidden'), 300); // Ẩn hoàn toàn sau 300ms
    });

    // Copy link chia sẻ vào clipboard
    document.getElementById('copyShareLink')?.addEventListener('click', function () {
        const shareLink = document.getElementById('shareLink');
        shareLink?.select(); // Chọn toàn bộ text
        document.execCommand('copy'); // Copy vào clipboard
        toast.show('Link copied to clipboard', 'success'); // Thông báo thành công
    });

    // ================ COPY SUGGESTIONS CARD ================
    // Chức năng copy gợi ý AI từ các card gợi ý
    // Mục đích: Cho phép người dùng sao chép nhanh các gợi ý AI vào clipboard
    document.querySelectorAll('.copy-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const suggestionText = btn.closest('.suggestion-card').querySelector('.ai-suggestion-text'); // Tìm text gợi ý trong card
            if (suggestionText) {
                const text = suggestionText.innerText || suggestionText.textContent; // Lấy nội dung text
                navigator.clipboard.writeText(text).then(() => {
                    toast.show('Copied suggestions to clipboard', 'success'); // Thông báo copy thành công
                });
            }
        });
    });

    // ================ CONTENT CARD MODAL HANDLING ================
    // Quản lý modal hiển thị chi tiết nội dung CV
    // Mục đích: Cho phép người dùng xem nội dung đầy đủ khi nội dung bị cắt ngắn
    const contentDetailModal = {
        element: document.getElementById('contentDetailModal'), // Element modal chính
        title: document.getElementById('modalTitle'), // Element hiển thị tiêu đề
        content: document.getElementById('modalContent'), // Element hiển thị nội dung
        closeBtn: document.getElementById('closeContentModal'), // Nút đóng modal

        // Hiển thị modal với animation
        show: function (title, content) {
            this.title.textContent = title; // Cập nhật tiêu đề
            this.content.innerHTML = content; // Cập nhật nội dung
            this.element.classList.remove('hidden'); // Hiển thị modal
            this.element.style.display = 'flex'; // Sử dụng flexbox để căn giữa
            setTimeout(() => this.element.classList.add('show'), 10); // Thêm animation sau 10ms
        },

        // Ẩn modal với animation
        hide: function () {
            this.element.classList.remove('show'); // Bắt đầu ẩn
            setTimeout(() => {
                this.element.style.display = 'none'; // Ẩn hoàn toàn
                this.element.classList.add('hidden'); // Thêm class hidden
            }, 300); // Sau 300ms (thời gian animation)
        },

        // Khởi tạo modal và gắn các event listener
        init: function () {
            if (this.closeBtn) {
                this.closeBtn.addEventListener('click', () => this.hide()); // Đóng khi click nút X
            }
            
            // Đóng modal khi click bên ngoài modal
            this.element.addEventListener('click', (e) => {
                if (e.target === this.element) {
                    this.hide(); // Chỉ đóng khi click vào background, không phải content
                }
            });

            // Đóng modal bằng phím Escape
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape' && !this.element.classList.contains('hidden')) {
                    this.hide(); // Đóng modal nếu đang hiển thị
                }
            });
        }
    };

    // Khởi tạo content detail modal
    contentDetailModal.init();

    // Xử lý click vào content card (toàn bộ card)
    document.querySelectorAll('.content-card').forEach(card => {
        card.addEventListener('click', function (e) {
            // Không trigger nếu click vào nút show-more (để tránh conflict)
            if (e.target.closest('.show-more-btn')) {
                e.stopPropagation(); // Dừng event bubbling
                return;
            }

            const fullContent = this.getAttribute('data-full-content'); // Lấy nội dung đầy đủ
            const title = this.getAttribute('data-title'); // Lấy tiêu đề
            
            if (fullContent && title) {
                contentDetailModal.show(title, fullContent); // Hiển thị modal với nội dung đầy đủ
            }
        });
    });

    // Xử lý click vào nút "View Details" (show-more button)
    document.querySelectorAll('.show-more-btn').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.stopPropagation(); // Dừng event bubbling để không trigger card click
            const card = this.closest('.content-card'); // Tìm card chứa nút
            const fullContent = card.getAttribute('data-full-content'); // Lấy nội dung đầy đủ
            const title = card.getAttribute('data-title'); // Lấy tiêu đề
            
            if (fullContent && title) {
                contentDetailModal.show(title, fullContent); // Hiển thị modal với nội dung đầy đủ
            }
        });
    });

    // ================ ENHANCED LOADING MODAL WITH PROGRESS ================
    // Nâng cấp modal loading với thanh tiến trình để tạo trải nghiệm tốt hơn
    // Mục đích: Hiển thị tiến trình xử lý thay vì chỉ spinner đơn giản
    let progressInterval = null; // Interval để cập nhật progress
    let progress = 0; // Giá trị progress hiện tại
    const progressBar = document.getElementById('progressBar'); // Element thanh tiến trình
    // Patch loadingModal.show/hide để thêm tính năng progress
    const originalShow = loadingModal.show.bind(loadingModal); // Lưu function show gốc
    const originalHide = loadingModal.hide.bind(loadingModal); // Lưu function hide gốc
    loadingModal.show = function () {
        if (progressBar) progressBar.style.width = '0%'; // Reset progress về 0%
        progress = 0; // Reset biến progress
        if (progressBar) progressBar.classList.add('animate-progress'); // Thêm animation cho progress bar
        if (loadingText) loadingText.innerHTML = `<div class="text-gray-800">Analyzing your CV...</div><div class="text-sm text-gray-500 mt-1 animate-pulse">This may take a few moments</div>`; // Cập nhật text loading
        originalShow(); // Gọi function show gốc
        if (progressBar) {
            progressInterval = setInterval(() => {
                progress += Math.random() * 10; // Tăng progress ngẫu nhiên 0-10%
                if (progress > 90) progress = 90; // Giới hạn tối đa 90% (để chờ kết quả thực)
                progressBar.style.width = `${progress}%`; // Cập nhật width của progress bar
            }, 300); // Cập nhật mỗi 300ms
        }
    };
    loadingModal.hide = function () {
        if (progressBar) progressBar.classList.remove('animate-progress'); // Xóa animation
        if (progressBar) progressBar.style.width = '100%'; // Hoàn thành 100%
        if (progressInterval) clearInterval(progressInterval); // Dừng interval
        setTimeout(() => {
            if (progressBar) progressBar.style.width = '0%'; // Reset về 0% sau 400ms
        }, 400);
        originalHide(); // Gọi function hide gốc
    };
    // Logic cho nút Cancel (reset progress)
    if (cancelBtn) {
        cancelBtn.addEventListener('click', function () {
            if (progressInterval) clearInterval(progressInterval); // Dừng interval
            if (progressBar) progressBar.classList.remove('animate-progress'); // Xóa animation
            if (progressBar) progressBar.style.width = '0%'; // Reset progress về 0%
        });
    }

    // Xử lý nút Try Again cho gợi ý công việc bằng event delegation
    // Đảm bảo luôn hoạt động kể cả khi nút được render lại động (dynamic rendering)
    // Mục đích: Cho phép người dùng thử lại gợi ý công việc từ kết quả session

    document.addEventListener('click', function (e) {
        if (e.target && e.target.id === 'btnJobTryAgain') {
            const resultDiv = document.getElementById('jobRecommendResultFromSession'); // Element hiển thị kết quả từ session
            const initialDiv = document.getElementById('jobInitialState'); // Element trạng thái ban đầu
            if (resultDiv) resultDiv.style.display = 'none'; // Ẩn kết quả từ session
            if (initialDiv) initialDiv.style.display = 'block'; // Hiển thị trạng thái ban đầu
            fetch('/CVAnalysis/ClearJobRecommendationSession', { method: 'POST' }); // Xóa dữ liệu session
        }
    });

    // ================ KHỞI TẠO TRẠNG THÁI BAN ĐẦU ================
    // Kiểm tra và thiết lập trạng thái hiển thị ban đầu cho CV Scoring
    // Mục đích: Đảm bảo chỉ hiển thị button khi chưa có CV, hiển thị bảng điểm khi đã có CV
    
    function initializeCVScoringState() {
        const btnScoreCV = document.getElementById('btnScoreCV');
        const scoreTable = document.getElementById('cv-score-table');
        const resumeId = btnScoreCV ? btnScoreCV.getAttribute('data-resumeid') : '';
        const cvContentInput = document.querySelector('input[name="Content"]');
        const cvContent = cvContentInput ? cvContentInput.value : '';
        
        // Kiểm tra xem có CV hay không
        const hasCV = resumeId && resumeId.trim() !== '' && cvContent && cvContent.trim() !== '';
        
        if (btnScoreCV && scoreTable) {
            if (hasCV) {
                // Kiểm tra xem CV này đã được chấm điểm chưa
                const hasBeenScored = resumeId && sessionStorage.getItem('cv_scored_' + resumeId) === 'true';
                
                if (hasBeenScored) {
                    // Nếu đã chấm điểm → Hiển thị kết quả, ẩn button
                    btnScoreCV.style.display = 'none';
                    scoreTable.style.display = 'block';
                } else {
                    // Bước 5: Nếu có CV nhưng chưa chấm điểm → Chỉ hiển thị Button
                    btnScoreCV.style.display = 'block';
                    scoreTable.style.display = 'none';
                }
            } else {
                // Bước 2: Nếu không có CV → Chỉ hiển thị button
                btnScoreCV.style.display = 'block';
                scoreTable.style.display = 'none';
            }
        }
    }

        // Gọi hàm khởi tạo khi trang load xong
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeCVScoringState);
    } else {
        initializeCVScoringState();
    }


});