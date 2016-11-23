%% Error Model

clear
clc
close all

%% 5% error
mag_error = 0.05;
ang_error = 5;
V_quant = floor(345000/(2^14)/sqrt(3));

%% System Info
bus_number_set_345KV_struct = load('Bus_number_set_345KV');
bus_number_set_345KV = bus_number_set_345KV_struct.bus_number_set;
bus_num_345KV = size(bus_number_set_345KV,1);

line_bus_info_all_lines_struct = load('line_bus_info_all_lines.mat');
line_bus_info_all_lines = line_bus_info_all_lines_struct.line_bus_info_all_lines;

line_bus_info_trn_struct = load('line_bus_info_trn.mat');
line_bus_info_trn = line_bus_info_trn_struct.line_bus_info_trn;

line_bus_info_gen_struct = load('line_bus_info_gen.mat');
line_bus_info_gen = line_bus_info_gen_struct.line_bus_info_gen;
% line_bus_info_gen = [line_bus_info_gen(:,1), -1*(1:size(line_bus_info_gen,1))'];

% line_bus_info_combined = [line_bus_info_all_lines; line_bus_info_trn ; line_bus_info_trn3];
line_all_lines_num = size(line_bus_info_all_lines,1);
line_trn_num = size(line_bus_info_trn,1);
line_gen_num = size(line_bus_info_gen,1);

line_num = line_all_lines_num + line_trn_num + line_gen_num;
line_number_set = (1:1:line_num)';

% find all 345KV lines' info
% line_bus_info_345KV_unique = [];
% line_bus_info_all_unique_lines = line_bus_info_all_lines(line_bus_info_all_lines(:,3)==1,:);
% for idx=1:size(line_bus_info_all_unique_lines,1)
%     from_bus_number = line_bus_info_all_unique_lines(idx,1);
%     to_bus_number = line_bus_info_all_unique_lines(idx,2);
%     
%     if sum(find(bus_number_set_345KV==from_bus_number))&&sum(find(bus_number_set_345KV==to_bus_number))
%         line_bus_info_345KV_unique = [line_bus_info_345KV_unique; line_bus_info_all_unique_lines(idx,:)];
%     end
% end

line_bus_info_345KV = [];
for idx=1:size(line_bus_info_all_lines,1)
    from_bus_number = line_bus_info_all_lines(idx,1);
    to_bus_number = line_bus_info_all_lines(idx,2);
    
    if sum(find(bus_number_set_345KV==from_bus_number))&&sum(find(bus_number_set_345KV==to_bus_number))
        line_bus_info_345KV = [line_bus_info_345KV; line_bus_info_all_lines(idx,:)];
    end
end
save('line_bus_info_345KV.mat', 'line_bus_info_345KV')
%% Voltage and Current Raw Data
raw_data_struct =  load('V_true_value_positive_sequence.mat');
raw_data_voltage = raw_data_struct.raw_data;

raw_data_current_struct = load('I_true_value_positive_sequence.mat');
raw_data_current = raw_data_current_struct.raw_data_current;

raw_data_current_trn_struct = load('I_true_value_positive_sequence_trn.mat');
raw_data_current_trn = raw_data_current_trn_struct.raw_data_current_trn;

raw_data_current_gen_struct = load('I_true_value_positive_sequence_gen.mat');
raw_data_current_gen = raw_data_current_gen_struct.raw_data_current_gen;


%% Record Line info: AC_line_info = [line number, line ID, line type, from bus number, KV1, KI1, to bus number, KV2, KI2, Z, y] 
%line ID is the ID information in line_bus_info files
%line type = 1 - transmission line; 2 - 2-winding transformer; 3 - 3-winding transformer
% And Generate line.mat data files 

AC_line_info = zeros(line_num,11);
three_phase_RE_record = zeros(line_num,12);


%% transmission lines
for idx1=1: line_all_lines_num%size(line_bus_info_all_lines,1)
    
    current_line_number = line_number_set(idx1,1);
    temp_line_info = line_bus_info_all_lines(idx1,:);
    
    if idx1 == 11
        a=1;
    end
    
    % set line number, from bus number, to bus number; Z and y are to be
    % set later
    AC_line_info(idx1,1) = current_line_number;        
    AC_line_info(idx1,2) = temp_line_info(1,3);
    AC_line_info(idx1,3) = 1;
    AC_line_info(idx1,4) = temp_line_info(1,1);
    AC_line_info(idx1,7) = temp_line_info(1,2);
    AC_line_info(idx1,10) = 0;
    AC_line_info(idx1,11) = 0; 

    % get accurate positive sequence voltage and current data for the current line and save
    from_bus_number = AC_line_info(idx1,4);
    to_bus_number = AC_line_info(idx1,7);

    V_from_bus_true_M = raw_data_voltage(:,(2*find(bus_number_set_345KV(:,1)==from_bus_number)-1));
    V_from_bus_true_A = raw_data_voltage(:,(2*find(bus_number_set_345KV(:,1)==from_bus_number)));
    if isempty(V_from_bus_true_M)
        V_from_bus_true_M = zeros(1800,1);
        V_from_bus_true_A = zeros(1800,1);
    end
    V_from_bus_true = V_from_bus_true_M.*cos(V_from_bus_true_A)+1i*V_from_bus_true_M.*sin(V_from_bus_true_A);
    
    V_to_bus_true_M = raw_data_voltage(:,(2*find(bus_number_set_345KV(:,1)==to_bus_number)-1));
    V_to_bus_true_A = raw_data_voltage(:,(2*find(bus_number_set_345KV(:,1)==to_bus_number)));
    V_to_bus_true = V_to_bus_true_M.*cos(V_to_bus_true_A)+1i*V_to_bus_true_M.*sin(V_to_bus_true_A);
    if isempty(V_to_bus_true_M)
        V_to_bus_true_M = zeros(1800,1);
        V_to_bus_true_A = zeros(1800,1);
    end
    
    if (from_bus_number==314900)||(to_bus_number==314905)
        a=1;
    end

    I_from_bus_true = raw_data_current(:,(2*idx1-1));
    I_to_bus_true = raw_data_current(:,(2*idx1));

    VI_true_set = [V_from_bus_true, I_from_bus_true, V_to_bus_true, I_to_bus_true];
    filename = ['line_', num2str(AC_line_info(idx1,1)), '_true_positive_sequence.mat'];
    save(filename,'VI_true_set');

    % get three-phase true VI data and save
    V_from_bus_A = V_from_bus_true;
    V_from_bus_B = V_from_bus_true.*exp(-1i*2/3*pi);
    V_from_bus_C = V_from_bus_true.*exp(1i*2/3*pi);
    V_to_bus_A = V_to_bus_true;
    V_to_bus_B = V_to_bus_true.*exp(-1i*2/3*pi);
    V_to_bus_C = V_to_bus_true.*exp(1i*2/3*pi);

    I_from_bus_A = I_from_bus_true;
    I_from_bus_B = I_from_bus_true.*exp(-1i*2/3*pi);
    I_from_bus_C = I_from_bus_true.*exp(1i*2/3*pi);
    I_to_bus_A = I_to_bus_true;
    I_to_bus_B = I_to_bus_true.*exp(-1i*2/3*pi);
    I_to_bus_C = I_to_bus_true.*exp(1i*2/3*pi);

    VI_true_3_phase_set = [V_from_bus_A, V_from_bus_B, V_from_bus_C, I_from_bus_A, I_from_bus_B, I_from_bus_C,...
        V_to_bus_A, V_to_bus_B, V_to_bus_C, I_to_bus_A, I_to_bus_B, I_to_bus_C];
    filename = ['line_', num2str(AC_line_info(idx1,1)), '_true_3_phase.mat'];
    save(filename,'VI_true_3_phase_set');

    % generate ratio errors and save
    RE_V_A_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_B_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_C_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_PS_from_bus = (RE_V_A_from_bus+RE_V_B_from_bus+RE_V_C_from_bus)/3;
    AC_line_info(idx1,5) = 1/RE_V_PS_from_bus;

    RE_I_A_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_B_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_C_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_PS_from_bus = (RE_I_A_from_bus+RE_I_B_from_bus+RE_I_C_from_bus)/3;
    AC_line_info(idx1,6) = 1/RE_I_PS_from_bus;

    RE_V_A_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_B_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_C_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_PS_to_bus = (RE_V_A_to_bus+RE_V_B_to_bus+RE_V_C_to_bus)/3;
    AC_line_info(idx1,8) = 1/RE_V_PS_to_bus;

    RE_I_A_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_B_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_C_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_PS_to_bus = (RE_I_A_to_bus+RE_I_B_to_bus+RE_I_C_to_bus)/3;
    AC_line_info(idx1,9) = 1/RE_I_PS_to_bus;

    three_phase_RE_record(idx1,:) = [RE_V_A_from_bus, RE_V_B_from_bus, RE_V_C_from_bus, RE_I_A_from_bus, RE_I_B_from_bus, RE_I_C_from_bus...
        RE_V_A_to_bus, RE_V_B_to_bus, RE_V_C_to_bus, RE_I_A_to_bus, RE_I_B_to_bus, RE_I_C_to_bus];

    % add ratio errors and quantize to get the measured positive sequence
    % data
    V_from_bus_A_w_RE = V_from_bus_A*RE_V_A_from_bus;
    V_from_bus_B_w_RE = V_from_bus_B*RE_V_B_from_bus;
    V_from_bus_C_w_RE = V_from_bus_C*RE_V_C_from_bus;
    V_from_bus_A_w_RE_quant = quant(real(V_from_bus_A_w_RE),V_quant)+1i*quant(imag(V_from_bus_A_w_RE),V_quant);
    V_from_bus_B_w_RE_quant = quant(real(V_from_bus_B_w_RE),V_quant)+1i*quant(imag(V_from_bus_B_w_RE),V_quant);
    V_from_bus_C_w_RE_quant = quant(real(V_from_bus_C_w_RE),V_quant)+1i*quant(imag(V_from_bus_C_w_RE),V_quant);
    V_from_bus_measured = (V_from_bus_A_w_RE_quant + V_from_bus_B_w_RE_quant*exp(1i*2/3*pi) + V_from_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    I_from_bus_A_w_RE = I_from_bus_A*RE_I_A_from_bus;
    I_from_bus_B_w_RE = I_from_bus_B*RE_I_B_from_bus;
    I_from_bus_C_w_RE = I_from_bus_C*RE_I_C_from_bus;
    I_from_bus_A_w_RE_quant = quant(real(I_from_bus_A_w_RE),0.65)+1i*quant(imag(I_from_bus_A_w_RE),0.65);
    I_from_bus_B_w_RE_quant = quant(real(I_from_bus_B_w_RE),0.65)+1i*quant(imag(I_from_bus_B_w_RE),0.65);
    I_from_bus_C_w_RE_quant = quant(real(I_from_bus_C_w_RE),0.65)+1i*quant(imag(I_from_bus_C_w_RE),0.65);
    I_from_bus_measured = (I_from_bus_A_w_RE_quant + I_from_bus_B_w_RE_quant*exp(1i*2/3*pi) + I_from_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    V_to_bus_A_w_RE = V_to_bus_A*RE_V_A_to_bus;
    V_to_bus_B_w_RE = V_to_bus_B*RE_V_B_to_bus;
    V_to_bus_C_w_RE = V_to_bus_C*RE_V_C_to_bus;
    V_to_bus_A_w_RE_quant = quant(real(V_to_bus_A_w_RE),V_quant)+1i*quant(imag(V_to_bus_A_w_RE),V_quant);
    V_to_bus_B_w_RE_quant = quant(real(V_to_bus_B_w_RE),V_quant)+1i*quant(imag(V_to_bus_B_w_RE),V_quant);
    V_to_bus_C_w_RE_quant = quant(real(V_to_bus_C_w_RE),V_quant)+1i*quant(imag(V_to_bus_C_w_RE),V_quant);
    V_to_bus_measured = (V_to_bus_A_w_RE_quant + V_to_bus_B_w_RE_quant*exp(1i*2/3*pi) + V_to_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    I_to_bus_A_w_RE = I_to_bus_A*RE_I_A_to_bus;
    I_to_bus_B_w_RE = I_to_bus_B*RE_I_B_to_bus;
    I_to_bus_C_w_RE = I_to_bus_C*RE_I_C_to_bus;
    I_to_bus_A_w_RE_quant = quant(real(I_to_bus_A_w_RE),0.65)+1i*quant(imag(I_to_bus_A_w_RE),0.65);
    I_to_bus_B_w_RE_quant = quant(real(I_to_bus_B_w_RE),0.65)+1i*quant(imag(I_to_bus_B_w_RE),0.65);
    I_to_bus_C_w_RE_quant = quant(real(I_to_bus_C_w_RE),0.65)+1i*quant(imag(I_to_bus_C_w_RE),0.65);
    I_to_bus_measured = (I_to_bus_A_w_RE_quant + I_to_bus_B_w_RE_quant*exp(1i*2/3*pi) + I_to_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    VI_measurement_set = [V_from_bus_measured, I_from_bus_measured, V_to_bus_measured, I_to_bus_measured];
    filename = ['line_', num2str(AC_line_info(idx1,1)), '_measured_positive_sequence.mat'];
    save(filename,'VI_measurement_set');  
    
    message = ['Line ', num2str(AC_line_info(idx1,1)), ' ID', num2str(AC_line_info(idx1,2)), ' completed.'];
    disp(message);
    
end

%% 2-winding transformers
for idx2=1: line_trn_num%size(line_bus_info_all_lines,1)
    
    idx2_in_total = idx2 + line_all_lines_num; % shift to 2-winding transformers
    
    current_line_number = line_number_set(idx2_in_total,1);
    temp_line_info = line_bus_info_trn(idx2,:);
    
    % all 2-winding transformers' currents add full 3-phase ratio error
    
    % set line number, from bus number, to bus number, Z and y
    AC_line_info(idx2_in_total,1) = current_line_number;        
    AC_line_info(idx2_in_total,2) = temp_line_info(1,3);
    AC_line_info(idx2_in_total,3) = 2;
    AC_line_info(idx2_in_total,4) = temp_line_info(1,1);
    AC_line_info(idx2_in_total,7) = temp_line_info(1,2);

    % get accurate positive sequence voltage and current data for the current line and save
    from_bus_number = AC_line_info(idx2_in_total,4);
    to_bus_number = AC_line_info(idx2_in_total,7);

    I_from_bus_true = raw_data_current_trn(:,(2*idx2-1));
    I_to_bus_true = raw_data_current_trn(:,(2*idx2));

    V_from_bus_true = zeros(size(I_from_bus_true,1),1);
    V_to_bus_true = zeros(size(I_from_bus_true,1),1);

    VI_true_set = [V_from_bus_true, I_from_bus_true, V_to_bus_true, I_to_bus_true];
    filename = ['line_', num2str(AC_line_info(idx2_in_total,1)), '_true_positive_sequence.mat'];
    save(filename,'VI_true_set');

    % get three-phase true VI data and save
    V_from_bus_A = V_from_bus_true;
    V_from_bus_B = V_from_bus_true.*exp(-1i*2/3*pi);
    V_from_bus_C = V_from_bus_true.*exp(1i*2/3*pi);
    V_to_bus_A = V_to_bus_true;
    V_to_bus_B = V_to_bus_true.*exp(-1i*2/3*pi);
    V_to_bus_C = V_to_bus_true.*exp(1i*2/3*pi);

    I_from_bus_A = I_from_bus_true;
    I_from_bus_B = I_from_bus_true.*exp(-1i*2/3*pi);
    I_from_bus_C = I_from_bus_true.*exp(1i*2/3*pi);
    I_to_bus_A = I_to_bus_true;
    I_to_bus_B = I_to_bus_true.*exp(-1i*2/3*pi);
    I_to_bus_C = I_to_bus_true.*exp(1i*2/3*pi);

    VI_true_3_phase_set = [V_from_bus_A, V_from_bus_B, V_from_bus_C, I_from_bus_A, I_from_bus_B, I_from_bus_C,...
        V_to_bus_A, V_to_bus_B, V_to_bus_C, I_to_bus_A, I_to_bus_B, I_to_bus_C];
    filename = ['line_', num2str(AC_line_info(idx2_in_total,1)), '_true_3_phase.mat'];
    save(filename,'VI_true_3_phase_set');

    % generate ratio errors and save
    RE_V_A_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_B_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_C_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_PS_from_bus = (RE_V_A_from_bus+RE_V_B_from_bus+RE_V_C_from_bus)/3;
    AC_line_info(idx2_in_total,5) = 1/RE_V_PS_from_bus;

    RE_I_A_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_B_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_C_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_PS_from_bus = (RE_I_A_from_bus+RE_I_B_from_bus+RE_I_C_from_bus)/3;
    AC_line_info(idx2_in_total,6) = 1/RE_I_PS_from_bus;

    RE_V_A_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_B_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_C_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_PS_to_bus = (RE_V_A_to_bus+RE_V_B_to_bus+RE_V_C_to_bus)/3;
    AC_line_info(idx2_in_total,8) = 1/RE_V_PS_to_bus;

    RE_I_A_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_B_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_C_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_PS_to_bus = (RE_I_A_to_bus+RE_I_B_to_bus+RE_I_C_to_bus)/3;
    AC_line_info(idx2_in_total,9) = 1/RE_I_PS_to_bus;

    three_phase_RE_record(idx2,:) = [RE_V_A_from_bus, RE_V_B_from_bus, RE_V_C_from_bus, RE_I_A_from_bus, RE_I_B_from_bus, RE_I_C_from_bus...
        RE_V_A_to_bus, RE_V_B_to_bus, RE_V_C_to_bus, RE_I_A_to_bus, RE_I_B_to_bus, RE_I_C_to_bus];

    % add ratio errors and quantize to get the measured positive sequence
    % data
    V_from_bus_A_w_RE = V_from_bus_A*RE_V_A_from_bus;
    V_from_bus_B_w_RE = V_from_bus_B*RE_V_B_from_bus;
    V_from_bus_C_w_RE = V_from_bus_C*RE_V_C_from_bus;
    V_from_bus_A_w_RE_quant = quant(real(V_from_bus_A_w_RE),V_quant)+1i*quant(imag(V_from_bus_A_w_RE),V_quant);
    V_from_bus_B_w_RE_quant = quant(real(V_from_bus_B_w_RE),V_quant)+1i*quant(imag(V_from_bus_B_w_RE),V_quant);
    V_from_bus_C_w_RE_quant = quant(real(V_from_bus_C_w_RE),V_quant)+1i*quant(imag(V_from_bus_C_w_RE),V_quant);
    V_from_bus_measured = (V_from_bus_A_w_RE_quant + V_from_bus_B_w_RE_quant*exp(1i*2/3*pi) + V_from_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    I_from_bus_A_w_RE = I_from_bus_A*RE_I_A_from_bus;
    I_from_bus_B_w_RE = I_from_bus_B*RE_I_B_from_bus;
    I_from_bus_C_w_RE = I_from_bus_C*RE_I_C_from_bus;
    I_from_bus_A_w_RE_quant = quant(real(I_from_bus_A_w_RE),0.65)+1i*quant(imag(I_from_bus_A_w_RE),0.65);
    I_from_bus_B_w_RE_quant = quant(real(I_from_bus_B_w_RE),0.65)+1i*quant(imag(I_from_bus_B_w_RE),0.65);
    I_from_bus_C_w_RE_quant = quant(real(I_from_bus_C_w_RE),0.65)+1i*quant(imag(I_from_bus_C_w_RE),0.65);
    I_from_bus_measured = (I_from_bus_A_w_RE_quant + I_from_bus_B_w_RE_quant*exp(1i*2/3*pi) + I_from_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    V_to_bus_A_w_RE = V_to_bus_A*RE_V_A_to_bus;
    V_to_bus_B_w_RE = V_to_bus_B*RE_V_B_to_bus;
    V_to_bus_C_w_RE = V_to_bus_C*RE_V_C_to_bus;
    V_to_bus_A_w_RE_quant = quant(real(V_to_bus_A_w_RE),V_quant)+1i*quant(imag(V_to_bus_A_w_RE),V_quant);
    V_to_bus_B_w_RE_quant = quant(real(V_to_bus_B_w_RE),V_quant)+1i*quant(imag(V_to_bus_B_w_RE),V_quant);
    V_to_bus_C_w_RE_quant = quant(real(V_to_bus_C_w_RE),V_quant)+1i*quant(imag(V_to_bus_C_w_RE),V_quant);
    V_to_bus_measured = (V_to_bus_A_w_RE_quant + V_to_bus_B_w_RE_quant*exp(1i*2/3*pi) + V_to_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    I_to_bus_A_w_RE = I_to_bus_A*RE_I_A_to_bus;
    I_to_bus_B_w_RE = I_to_bus_B*RE_I_B_to_bus;
    I_to_bus_C_w_RE = I_to_bus_C*RE_I_C_to_bus;
    I_to_bus_A_w_RE_quant = quant(real(I_to_bus_A_w_RE),0.65)+1i*quant(imag(I_to_bus_A_w_RE),0.65);
    I_to_bus_B_w_RE_quant = quant(real(I_to_bus_B_w_RE),0.65)+1i*quant(imag(I_to_bus_B_w_RE),0.65);
    I_to_bus_C_w_RE_quant = quant(real(I_to_bus_C_w_RE),0.65)+1i*quant(imag(I_to_bus_C_w_RE),0.65);
    I_to_bus_measured = (I_to_bus_A_w_RE_quant + I_to_bus_B_w_RE_quant*exp(1i*2/3*pi) + I_to_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    VI_measurement_set = [V_from_bus_measured, I_from_bus_measured, V_to_bus_measured, I_to_bus_measured];
    filename = ['line_', num2str(AC_line_info(idx2_in_total,1)), '_measured_positive_sequence.mat'];
    save(filename,'VI_measurement_set');  
    
    
    message = ['Line ', num2str(AC_line_info(idx2_in_total,1)), ' ID', num2str(AC_line_info(idx2_in_total,2)), ' completed.'];
    disp(message);
    
end

%% Generators
for idx3=1: line_gen_num%size(line_bus_info_all_lines,1)
    
    idx3_in_total = idx3 + line_all_lines_num + line_trn_num; % shift to 3-winding transformers
    
    current_line_number = line_number_set(idx3_in_total,1);
    temp_line_info = line_bus_info_gen(idx3,:);
    
    % all 2-winding transformers' currents add full 3-phase ratio error
    
    % set line number, from bus number, to bus number, Z and y
    AC_line_info(idx3_in_total,1) = current_line_number;        
    AC_line_info(idx3_in_total,2) = 1; % only one measurement for each 3-winding transformer
    AC_line_info(idx3_in_total,3) = 3;
    AC_line_info(idx3_in_total,4) = temp_line_info(1,1);
    AC_line_info(idx3_in_total,7) = temp_line_info(1,2);

    % get accurate positive sequence voltage and current data for the current line and save
    from_bus_number = AC_line_info(idx3_in_total,4);
    to_bus_number = AC_line_info(idx3_in_total,7);

    I_from_bus_true = raw_data_current_gen(:,idx3);
    I_to_bus_true = zeros(size(I_from_bus_true,1),1);

    V_from_bus_true = zeros(size(I_from_bus_true,1),1);
    V_to_bus_true = zeros(size(I_from_bus_true,1),1);

    VI_true_set = [V_from_bus_true, I_from_bus_true, V_to_bus_true, I_to_bus_true];
    filename = ['line_', num2str(AC_line_info(idx3_in_total,1)), '_true_positive_sequence.mat'];
    save(filename,'VI_true_set');

    % get three-phase true VI data and save
    V_from_bus_A = V_from_bus_true;
    V_from_bus_B = V_from_bus_true.*exp(-1i*2/3*pi);
    V_from_bus_C = V_from_bus_true.*exp(1i*2/3*pi);
    V_to_bus_A = V_to_bus_true;
    V_to_bus_B = V_to_bus_true.*exp(-1i*2/3*pi);
    V_to_bus_C = V_to_bus_true.*exp(1i*2/3*pi);

    I_from_bus_A = I_from_bus_true;
    I_from_bus_B = I_from_bus_true.*exp(-1i*2/3*pi);
    I_from_bus_C = I_from_bus_true.*exp(1i*2/3*pi);
    I_to_bus_A = I_to_bus_true;
    I_to_bus_B = I_to_bus_true.*exp(-1i*2/3*pi);
    I_to_bus_C = I_to_bus_true.*exp(1i*2/3*pi);

    VI_true_3_phase_set = [V_from_bus_A, V_from_bus_B, V_from_bus_C, I_from_bus_A, I_from_bus_B, I_from_bus_C,...
        V_to_bus_A, V_to_bus_B, V_to_bus_C, I_to_bus_A, I_to_bus_B, I_to_bus_C];
    filename = ['line_', num2str(AC_line_info(idx3_in_total,1)), '_true_3_phase.mat'];
    save(filename,'VI_true_3_phase_set');

    % generate ratio errors and save
    RE_V_A_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_B_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_C_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_PS_from_bus = (RE_V_A_from_bus+RE_V_B_from_bus+RE_V_C_from_bus)/3;
    AC_line_info(idx3_in_total,5) = 1/RE_V_PS_from_bus;

    RE_I_A_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_B_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_C_from_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_PS_from_bus = (RE_I_A_from_bus+RE_I_B_from_bus+RE_I_C_from_bus)/3;
    AC_line_info(idx3_in_total,6) = 1/RE_I_PS_from_bus;

    RE_V_A_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_B_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_C_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_V_PS_to_bus = (RE_V_A_to_bus+RE_V_B_to_bus+RE_V_C_to_bus)/3;
    AC_line_info(idx3_in_total,8) = 1/RE_V_PS_to_bus;

    RE_I_A_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_B_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_C_to_bus =(1+2*mag_error*(rand-0.5))*exp(1i*2*ang_error*(rand-0.5)*pi/180);
    RE_I_PS_to_bus = (RE_I_A_to_bus+RE_I_B_to_bus+RE_I_C_to_bus)/3;
    AC_line_info(idx3_in_total,9) = 1/RE_I_PS_to_bus;

    three_phase_RE_record(idx3,:) = [RE_V_A_from_bus, RE_V_B_from_bus, RE_V_C_from_bus, RE_I_A_from_bus, RE_I_B_from_bus, RE_I_C_from_bus...
        RE_V_A_to_bus, RE_V_B_to_bus, RE_V_C_to_bus, RE_I_A_to_bus, RE_I_B_to_bus, RE_I_C_to_bus];

    % add ratio errors and quantize to get the measured positive sequence
    % data
    V_from_bus_A_w_RE = V_from_bus_A*RE_V_A_from_bus;
    V_from_bus_B_w_RE = V_from_bus_B*RE_V_B_from_bus;
    V_from_bus_C_w_RE = V_from_bus_C*RE_V_C_from_bus;
    V_from_bus_A_w_RE_quant = quant(real(V_from_bus_A_w_RE),V_quant)+1i*quant(imag(V_from_bus_A_w_RE),V_quant);
    V_from_bus_B_w_RE_quant = quant(real(V_from_bus_B_w_RE),V_quant)+1i*quant(imag(V_from_bus_B_w_RE),V_quant);
    V_from_bus_C_w_RE_quant = quant(real(V_from_bus_C_w_RE),V_quant)+1i*quant(imag(V_from_bus_C_w_RE),V_quant);
    V_from_bus_measured = (V_from_bus_A_w_RE_quant + V_from_bus_B_w_RE_quant*exp(1i*2/3*pi) + V_from_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    I_from_bus_A_w_RE = I_from_bus_A*RE_I_A_from_bus;
    I_from_bus_B_w_RE = I_from_bus_B*RE_I_B_from_bus;
    I_from_bus_C_w_RE = I_from_bus_C*RE_I_C_from_bus;
    I_from_bus_A_w_RE_quant = quant(real(I_from_bus_A_w_RE),0.65)+1i*quant(imag(I_from_bus_A_w_RE),0.65);
    I_from_bus_B_w_RE_quant = quant(real(I_from_bus_B_w_RE),0.65)+1i*quant(imag(I_from_bus_B_w_RE),0.65);
    I_from_bus_C_w_RE_quant = quant(real(I_from_bus_C_w_RE),0.65)+1i*quant(imag(I_from_bus_C_w_RE),0.65);
    I_from_bus_measured = (I_from_bus_A_w_RE_quant + I_from_bus_B_w_RE_quant*exp(1i*2/3*pi) + I_from_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    V_to_bus_A_w_RE = V_to_bus_A*RE_V_A_to_bus;
    V_to_bus_B_w_RE = V_to_bus_B*RE_V_B_to_bus;
    V_to_bus_C_w_RE = V_to_bus_C*RE_V_C_to_bus;
    V_to_bus_A_w_RE_quant = quant(real(V_to_bus_A_w_RE),V_quant)+1i*quant(imag(V_to_bus_A_w_RE),V_quant);
    V_to_bus_B_w_RE_quant = quant(real(V_to_bus_B_w_RE),V_quant)+1i*quant(imag(V_to_bus_B_w_RE),V_quant);
    V_to_bus_C_w_RE_quant = quant(real(V_to_bus_C_w_RE),V_quant)+1i*quant(imag(V_to_bus_C_w_RE),V_quant);
    V_to_bus_measured = (V_to_bus_A_w_RE_quant + V_to_bus_B_w_RE_quant*exp(1i*2/3*pi) + V_to_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    I_to_bus_A_w_RE = I_to_bus_A*RE_I_A_to_bus;
    I_to_bus_B_w_RE = I_to_bus_B*RE_I_B_to_bus;
    I_to_bus_C_w_RE = I_to_bus_C*RE_I_C_to_bus;
    I_to_bus_A_w_RE_quant = quant(real(I_to_bus_A_w_RE),0.65)+1i*quant(imag(I_to_bus_A_w_RE),0.65);
    I_to_bus_B_w_RE_quant = quant(real(I_to_bus_B_w_RE),0.65)+1i*quant(imag(I_to_bus_B_w_RE),0.65);
    I_to_bus_C_w_RE_quant = quant(real(I_to_bus_C_w_RE),0.65)+1i*quant(imag(I_to_bus_C_w_RE),0.65);
    I_to_bus_measured = (I_to_bus_A_w_RE_quant + I_to_bus_B_w_RE_quant*exp(1i*2/3*pi) + I_to_bus_C_w_RE_quant*exp(-1i*2/3*pi))/3;

    VI_measurement_set = [V_from_bus_measured, I_from_bus_measured, V_to_bus_measured, I_to_bus_measured];
    filename = ['line_', num2str(AC_line_info(idx3_in_total,1)), '_measured_positive_sequence.mat'];
    save(filename,'VI_measurement_set');  
    
    
    message = ['Line ', num2str(AC_line_info(idx3_in_total,1)), ' ID', num2str(AC_line_info(idx3_in_total,2)), ' completed.'];
    disp(message);
    
end


%% Record line info and three phase ratio errors
save('AC_line_info.mat','AC_line_info');
save('three_phase_ratio_errors.mat','three_phase_RE_record');













